namespace Audit.Application.Models.QueryModels;

public sealed class AuditLogDetailResult
{
    public long AuditId { get; init; }

    public string AuditEventId { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; }

    public long? ActorUserId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourceId { get; init; } = string.Empty;

    public string? Outcome { get; init; }

    public string Summary { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public string? CorrelationId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? OldValuesJson { get; init; }

    public string? NewValuesJson { get; init; }

    public string? MetadataJson { get; init; }
}