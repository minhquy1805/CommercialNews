namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;

public sealed class OutboxWriteRequest
{
    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string AggregateType { get; init; } = string.Empty;

    public string AggregateId { get; init; } = string.Empty;

    public string? AggregatePublicId { get; init; }

    public int? AggregateVersion { get; init; }

    public string Payload { get; init; } = string.Empty;

    public string? Headers { get; init; }

    public string? CorrelationId { get; init; }

    public long? InitiatorUserId { get; init; }

    public byte Priority { get; init; } = 5;

    public DateTime OccurredAtUtc { get; init; }
}