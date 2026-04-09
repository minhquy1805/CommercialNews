namespace Notifications.Application.Contracts.Outbox.Responses;

public sealed class ProcessOutboxMessageResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public long? EmailDeliveryId { get; init; }

    public string OutboxStatus { get; init; } = string.Empty;

    public string? EmailDeliveryStatus { get; init; }

    public bool Processed { get; init; }
}