namespace CommercialNews.Api.Api.Admin.Contracts.Audit.Ingestion.Responses;

public sealed class AuditIngestionDetailHttpResponse
{
    public string PublicId { get; init; } = string.Empty;

    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string? AggregateType { get; init; }

    public string? AggregateId { get; init; }

    public string? AggregatePublicId { get; init; }

    public int? AggregateVersion { get; init; }

    public string? CorrelationId { get; init; }

    public int? SourcePriority { get; init; }

    public DateTime SourceOccurredAtUtc { get; init; }

    public DateTime? SourcePublishedAtUtc { get; init; }

    public string ConsumerName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTime FirstReceivedAtUtc { get; init; }

    public DateTime? LastAttemptAtUtc { get; init; }

    public DateTime? ProcessedAtUtc { get; init; }

    public DateTime? DeadLetteredAtUtc { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorMessage { get; init; }

    public string? LastErrorClass { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
