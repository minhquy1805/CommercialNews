namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;

public sealed class AuditLogListItemHttpResponse
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

    public string? CorrelationId { get; init; }
}