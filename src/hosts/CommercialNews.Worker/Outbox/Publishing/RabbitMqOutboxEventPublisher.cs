using System.Text;
using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.IntegrationEvents;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CommercialNews.Worker.Outbox.Publishing;

public sealed class RabbitMqOutboxEventPublisher : IOutboxEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OutboxRabbitMqOptions _options;
    private readonly ILogger<RabbitMqOutboxEventPublisher> _logger;

    public RabbitMqOutboxEventPublisher(
        IOptions<OutboxRabbitMqOptions> options,
        ILogger<RabbitMqOutboxEventPublisher> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PublishOutboxEventResult>> PublishAsync(
        OutboxMessage message,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            return Result<PublishOutboxEventResult>.Success(
                PublishOutboxEventResult.Failed(
                    errorCode: "OUTBOX.ROUTING_KEY_REQUIRED",
                    errorMessage: "RabbitMQ routing key is required.",
                    errorClass: OutboxFailureClass.Validation,
                    isRetryable: false));
        }

        try
        {
            using CancellationTokenSource timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutCts.CancelAfter(TimeSpan.FromSeconds(
                Math.Max(1, _options.PublishTimeoutSeconds)));

            await PublishCoreAsync(
                message,
                routingKey.Trim(),
                timeoutCts.Token);

            return Result<PublishOutboxEventResult>.Success(
                PublishOutboxEventResult.Success());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "RabbitMQ publish timed out. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, EventType={EventType}, RoutingKey={RoutingKey}",
                message.OutboxMessageId,
                message.MessageId,
                message.EventType,
                routingKey);

            return Result<PublishOutboxEventResult>.Success(
                PublishOutboxEventResult.Failed(
                    errorCode: "OUTBOX.RABBITMQ_PUBLISH_TIMEOUT",
                    errorMessage: "RabbitMQ publish timed out.",
                    errorClass: OutboxFailureClass.Transient,
                    isRetryable: true,
                    isAmbiguous: true));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "RabbitMQ publish failed. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, EventType={EventType}, RoutingKey={RoutingKey}",
                message.OutboxMessageId,
                message.MessageId,
                message.EventType,
                routingKey);

            return Result<PublishOutboxEventResult>.Success(
                PublishOutboxEventResult.Failed(
                    errorCode: "OUTBOX.RABBITMQ_PUBLISH_FAILED",
                    errorMessage: exception.Message,
                    errorClass: OutboxFailureClass.Transient,
                    isRetryable: true));
        }
    }

    private async Task PublishCoreAsync(
        OutboxMessage message,
        string routingKey,
        CancellationToken cancellationToken)
    {
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
            await factory.CreateConnectionAsync(cancellationToken);

        await using IChannel channel =
            await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: _options.DurableExchange,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        OutboxIntegrationEventEnvelope envelope = CreateEnvelope(message);

        string json = JsonSerializer.Serialize(envelope, JsonOptions);
        byte[] body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            MessageId = message.MessageId,
            Type = message.EventType,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            CorrelationId = message.CorrelationId,
            Timestamp = new AmqpTimestamp(
                new DateTimeOffset(message.OccurredAt).ToUnixTimeSeconds()),
            DeliveryMode = _options.PersistentMessages
                ? DeliveryModes.Persistent
                : DeliveryModes.Transient
        };

        if (!string.IsNullOrWhiteSpace(message.Headers))
        {
            properties.Headers = new Dictionary<string, object?>
            {
                ["outbox.headers"] = message.Headers
            };
        }

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static OutboxIntegrationEventEnvelope CreateEnvelope(
        OutboxMessage message)
    {
        JsonElement payload =
            JsonSerializer.Deserialize<JsonElement>(message.Payload);

        JsonElement? headers = string.IsNullOrWhiteSpace(message.Headers)
            ? null
            : JsonSerializer.Deserialize<JsonElement>(message.Headers);

        return new OutboxIntegrationEventEnvelope(
            MessageId: message.MessageId,
            EventType: message.EventType,
            AggregateType: message.AggregateType,
            AggregateId: message.AggregateId,
            AggregatePublicId: message.AggregatePublicId,
            AggregateVersion: message.AggregateVersion,
            Payload: payload,
            Headers: headers,
            CorrelationId: message.CorrelationId,
            InitiatorUserId: message.InitiatorUserId,
            OccurredAtUtc: message.OccurredAt);
    }
}