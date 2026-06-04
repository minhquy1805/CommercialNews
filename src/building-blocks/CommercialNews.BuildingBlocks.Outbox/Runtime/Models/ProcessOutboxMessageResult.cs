namespace CommercialNews.BuildingBlocks.Outbox.Runtime.Models;

public sealed class ProcessOutboxMessageResult
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public bool Succeeded { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}