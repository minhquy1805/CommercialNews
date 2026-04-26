namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;

public sealed class GetOutboxMessageByIdResponse
{
    public long OutboxMessageId { get; init; }

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

    public byte Priority { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public DateTime? LastAttemptAt { get; init; }

    public DateTime? PublishedAt { get; init; }

    public string? LastError { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }

    public DateTime OccurredAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}