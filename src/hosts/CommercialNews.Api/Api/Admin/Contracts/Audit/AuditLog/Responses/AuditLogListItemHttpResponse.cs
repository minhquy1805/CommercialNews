using CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;

public sealed class AuditLogListItemHttpResponse
{
    public string PublicId { get; init; } = string.Empty;

    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string SourceModule { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string? ActionCategory { get; init; }

    public AuditActorHttpResponse Actor { get; init; } = new();

    public AuditResourceHttpResponse Resource { get; init; } = new();

    public string Outcome { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string RiskLevel { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public DateTime IngestedAtUtc { get; init; }
}
