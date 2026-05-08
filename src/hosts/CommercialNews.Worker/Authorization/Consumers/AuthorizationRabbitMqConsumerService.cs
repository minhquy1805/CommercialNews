using System.Text;
using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.Worker.Authorization.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommercialNews.Worker.Authorization.Consumers;

public sealed class AuthorizationRabbitMqConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AuthorizationRabbitMqConsumerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuthorizationRabbitMqConsumerService> _logger;

    public AuthorizationRabbitMqConsumerService(
        IOptions<AuthorizationRabbitMqConsumerOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<AuthorizationRabbitMqConsumerService> logger)
    {
        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));
        _scopeFactory = scopeFactory
            ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (_options.RoutingKeys.Length == 0)
        {
            _logger.LogWarning(
                "Authorization RabbitMQ consumer has no routing keys configured. Consumer will not start.");

            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            ClientProvidedName = _options.ClientProvidedName
        };

        await using IConnection connection =
            await factory.CreateConnectionAsync(stoppingToken);

        await using IChannel channel =
            await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareTopologyAsync(channel, stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleMessageAsync(
                channel,
                eventArgs,
                stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumerTag: _options.ConsumerTag,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Authorization RabbitMQ consumer started. QueueName={QueueName}, ExchangeName={ExchangeName}, RoutingKeys={RoutingKeys}",
            _options.QueueName,
            _options.ExchangeName,
            string.Join(",", _options.RoutingKeys));

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Authorization RabbitMQ consumer is stopping.");
        }
    }

    private async Task DeclareTopologyAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: _options.DurableQueue,
            exclusive: _options.ExclusiveQueue,
            autoDelete: _options.AutoDeleteQueue,
            arguments: null,
            cancellationToken: cancellationToken);

        foreach (string routingKey in _options.RoutingKeys)
        {
            if (string.IsNullOrWhiteSpace(routingKey))
            {
                continue;
            }

            await channel.QueueBindAsync(
                queue: _options.QueueName,
                exchange: _options.ExchangeName,
                routingKey: routingKey.Trim(),
                arguments: null,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        string rawMessage = Encoding.UTF8.GetString(eventArgs.Body.Span);

        OutboxIntegrationEventEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<OutboxIntegrationEventEnvelope>(
                rawMessage,
                JsonOptions);

            if (envelope is null)
            {
                _logger.LogWarning(
                    "Authorization consumer received an empty or invalid envelope. DeliveryTag={DeliveryTag}",
                    eventArgs.DeliveryTag);

                await AckAsync(channel, eventArgs, cancellationToken);
                return;
            }
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Authorization consumer failed to deserialize message body. DeliveryTag={DeliveryTag}",
                eventArgs.DeliveryTag);

            await AckAsync(channel, eventArgs, cancellationToken);
            return;
        }

        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

            AuthorizationIntegrationEventDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<AuthorizationIntegrationEventDispatcher>();

            var dispatchResult = await dispatcher.DispatchAsync(
                envelope,
                cancellationToken);

            if (dispatchResult.IsSuccess)
            {
                await AckAsync(channel, eventArgs, cancellationToken);

                _logger.LogInformation(
                    "Authorization consumer processed integration event. EventType={EventType}, MessageId={MessageId}, CorrelationId={CorrelationId}",
                    envelope.EventType,
                    envelope.MessageId,
                    envelope.CorrelationId);

                return;
            }

            _logger.LogWarning(
                "Authorization consumer handler failed. EventType={EventType}, MessageId={MessageId}, CorrelationId={CorrelationId}, ErrorCode={ErrorCode}",
                envelope.EventType,
                envelope.MessageId,
                envelope.CorrelationId,
                dispatchResult.Error?.Code);

            await NackAsync(
                channel,
                eventArgs,
                requeue: _options.RequeueOnFailure,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Authorization consumer failed unexpectedly. DeliveryTag={DeliveryTag}",
                eventArgs.DeliveryTag);

            await NackAsync(
                channel,
                eventArgs,
                requeue: _options.RequeueOnFailure,
                cancellationToken);
        }
    }

    private static async Task AckAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        await channel.BasicAckAsync(
            deliveryTag: eventArgs.DeliveryTag,
            multiple: false,
            cancellationToken: cancellationToken);
    }

    private static async Task NackAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        bool requeue,
        CancellationToken cancellationToken)
    {
        await channel.BasicNackAsync(
            deliveryTag: eventArgs.DeliveryTag,
            multiple: false,
            requeue: requeue,
            cancellationToken: cancellationToken);
    }
}
