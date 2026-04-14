namespace Audit.Application.Models.QueryModels;

public sealed class AuditLogListQuery
{
    public DateTime? FromOccurredAt { get; init; }

    public DateTime? ToOccurredAt { get; init; }

    public long? ActorUserId { get; init; }

    public string? Action { get; init; }

    public string? ResourceType { get; init; }

    public string? ResourceId { get; init; }

    public string? CorrelationId { get; init; }

    public string? AuditEventId { get; init; }

    public string? Outcome { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; } = 20;
}