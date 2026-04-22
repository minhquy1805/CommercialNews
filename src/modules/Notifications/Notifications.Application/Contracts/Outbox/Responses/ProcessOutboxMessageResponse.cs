namespace Notifications.Application.Contracts.Outbox.Responses;

public sealed class ProcessOutboxMessageResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public bool CreatedEmailDelivery { get; init; }

    public long? EmailDeliveryId { get; init; }

    public string Status { get; init; } = string.Empty;
}