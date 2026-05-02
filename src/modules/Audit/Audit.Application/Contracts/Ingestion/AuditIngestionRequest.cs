namespace Audit.Application.Contracts.Ingestion;

public sealed class AuditIngestionRequest
{
    public required string MessageId { get; init; }

    public required string Action { get; init; }

    public required string ResourceType { get; init; }

    public required string ResourceId { get; init; }

    public required string Summary { get; init; }

    public long? ActorUserId { get; init; }

    public string? Outcome { get; init; }

    public string? Reason { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public string? CorrelationId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? OldValuesJson { get; init; }

    public string? NewValuesJson { get; init; }

    public string? MetadataJson { get; init; }
}