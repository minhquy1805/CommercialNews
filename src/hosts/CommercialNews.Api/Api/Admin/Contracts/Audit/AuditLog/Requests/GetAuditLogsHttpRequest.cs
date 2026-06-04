namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Requests;

public sealed class GetAuditLogsHttpRequest
{
    public string? MessageId { get; init; }

    public string? SourceModule { get; init; }

    public string? EventType { get; init; }

    public string? Action { get; init; }

    public string? ActionCategory { get; init; }

    public string? ResourceType { get; init; }

    public string? ResourceId { get; init; }

    public string? ActorUserId { get; init; }

    public long? ActorInternalId { get; init; }

    public string? Outcome { get; init; }

    public string? Severity { get; init; }

    public string? RiskLevel { get; init; }

    public string? CorrelationId { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Sort { get; init; }
}
