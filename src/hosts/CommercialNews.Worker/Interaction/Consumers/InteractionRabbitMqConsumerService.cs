using System.Text;
using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.Worker.Interaction.Handlers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommercialNews.Worker.Interaction.Consumers;

public sealed class InteractionRabbitMqConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<InteractionRabbitMqConsumerOptions> _optionsMonitor;
    private readonly ILogger<InteractionRabbitMqConsumerService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public InteractionRabbitMqConsumerService(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<InteractionRabbitMqConsumerOptions> optionsMonitor,
        ILogger<InteractionRabbitMqConsumerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        _optionsMonitor = optionsMonitor
            ?? throw new ArgumentNullException(nameof(optionsMonitor));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    private InteractionRabbitMqConsumerOptions Options => _optionsMonitor.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InteractionRabbitMqConsumerOptions options = Options;
        ValidateRoutingKeys(options.RoutingKeys);

        _logger.LogInformation(
            "Interaction RabbitMQ consumer starting. Exchange={ExchangeName}, Queue={QueueName}, ConsumerTag={ConsumerTag}, PrefetchCount={PrefetchCount}",
            options.ExchangeName,
            options.QueueName,
            options.ConsumerTag,
            options.PrefetchCount);

        try
        {
            await StartConsumerAsync(options, stoppingToken);

            _logger.LogInformation(
                "Interaction RabbitMQ consumer started. Exchange={ExchangeName}, Queue={QueueName}, RoutingKeys={RoutingKeys}",
                options.ExchangeName,
                options.QueueName,
                string.Join(", ", options.RoutingKeys));

            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Interaction RabbitMQ consumer stopped because of an unhandled exception.");
        }
        finally
        {
            await DisposeRabbitMqAsync();

            _logger.LogInformation("Interaction RabbitMQ consumer stopped.");
        }
    }

    private static void ValidateRoutingKeys(
        string[]? routingKeys)
    {
        if (routingKeys is null
            || !routingKeys.Any(static routingKey => !string.IsNullOrWhiteSpace(routingKey)))
        {
            throw new InvalidOperationException(
                "Interaction RabbitMQ consumer requires at least one routing key.");
        }
    }

    private async Task StartConsumerAsync(
        InteractionRabbitMqConsumerOptions options,
        CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            ClientProvidedName = options.ClientProvidedName
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: options.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: options.DurableQueue,
            exclusive: options.ExclusiveQueue,
            autoDelete: options.AutoDeleteQueue,
            arguments: null,
            cancellationToken: cancellationToken);

        foreach (string routingKey in options.RoutingKeys)
        {
            if (string.IsNullOrWhiteSpace(routingKey))
            {
                continue;
            }

            await _channel.QueueBindAsync(
                queue: options.QueueName,
                exchange: options.ExchangeName,
                routingKey: routingKey.Trim(),
                arguments: null,
                cancellationToken: cancellationToken);
        }

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: options.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleDeliveryAsync(
                eventArgs,
                options,
                cancellationToken);
        };

        await _channel.BasicConsumeAsync(
            queue: options.QueueName,
            autoAck: false,
            consumerTag: options.ConsumerTag,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task HandleDeliveryAsync(
        BasicDeliverEventArgs eventArgs,
        InteractionRabbitMqConsumerOptions options,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            _logger.LogWarning(
                "Interaction consumer received a message but RabbitMQ channel is not available. DeliveryTag={DeliveryTag}",
                eventArgs.DeliveryTag);

            return;
        }

        OutboxIntegrationEventEnvelope? envelope = null;

        try
        {
            envelope = DeserializeEnvelope(eventArgs);

            if (envelope is null)
            {
                _logger.LogWarning(
                    "Interaction consumer received an empty or invalid envelope. DeliveryTag={DeliveryTag}, RoutingKey={RoutingKey}",
                    eventArgs.DeliveryTag,
                    eventArgs.RoutingKey);

                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: cancellationToken);

                return;
            }

            await using AsyncServiceScope scope =
                _serviceScopeFactory.CreateAsyncScope();

            var dispatcher =
                scope.ServiceProvider.GetRequiredService<InteractionIntegrationEventDispatcher>();

            var result = await dispatcher.DispatchAsync(
                envelope,
                cancellationToken);

            if (result.IsSuccess)
            {
                await _channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Interaction event consumed successfully. MessageId={MessageId}, EventType={EventType}, RoutingKey={RoutingKey}",
                    envelope.MessageId,
                    envelope.EventType,
                    eventArgs.RoutingKey);

                return;
            }

            var error = result.Error!;

            _logger.LogWarning(
                "Interaction event processing failed. MessageId={MessageId}, EventType={EventType}, RoutingKey={RoutingKey}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                envelope.MessageId,
                envelope.EventType,
                eventArgs.RoutingKey,
                error.Code,
                error.Message);

            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: options.RequeueOnFailure,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Interaction consumer failed to deserialize RabbitMQ message. DeliveryTag={DeliveryTag}, RoutingKey={RoutingKey}",
                eventArgs.DeliveryTag,
                eventArgs.RoutingKey);

            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Interaction consumer failed while processing message. MessageId={MessageId}, EventType={EventType}, RoutingKey={RoutingKey}, DeliveryTag={DeliveryTag}",
                envelope?.MessageId,
                envelope?.EventType,
                eventArgs.RoutingKey,
                eventArgs.DeliveryTag);

            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: options.RequeueOnFailure,
                cancellationToken: cancellationToken);
        }
    }

    private static OutboxIntegrationEventEnvelope? DeserializeEnvelope(
        BasicDeliverEventArgs eventArgs)
    {
        if (eventArgs.Body.IsEmpty)
        {
            return null;
        }

        string json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        return JsonSerializer.Deserialize<OutboxIntegrationEventEnvelope>(
            json,
            JsonOptions);
    }

    private async Task DisposeRabbitMqAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
