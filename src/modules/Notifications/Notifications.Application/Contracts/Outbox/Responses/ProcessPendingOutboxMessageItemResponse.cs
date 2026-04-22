namespace Notifications.Application.Contracts.Outbox.Responses;

public sealed class ProcessPendingOutboxMessageItemResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public bool Succeeded { get; init; }

    public bool CreatedEmailDelivery { get; init; }

    public long? EmailDeliveryId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}