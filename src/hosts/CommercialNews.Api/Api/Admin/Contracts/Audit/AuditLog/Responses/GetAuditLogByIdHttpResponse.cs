using CommercialNews.Api.Api.Admin.Contracts.Audit.Common;

namespace CommercialNews.Api.Api.Admin.Contracts.Audit.AuditLog.Responses;

public sealed class GetAuditLogByIdHttpResponse
{
    public string PublicId { get; init; } = string.Empty;

    public string MessageId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public int? EventVersion { get; init; }

    public string SourceModule { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string? ActionCategory { get; init; }

    public AuditAggregateHttpResponse Aggregate { get; init; } = new();

    public AuditActorHttpResponse Actor { get; init; } = new();

    public AuditResourceHttpResponse Resource { get; init; } = new();

    public string Outcome { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string RiskLevel { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public string? CorrelationId { get; init; }

    public string? CausationId { get; init; }

    public string? TraceId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public int? SourcePriority { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public DateTime IngestedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string? MetadataJson { get; init; }

    public string? HeadersJson { get; init; }

    public string? SanitizedPayloadJson { get; init; }

    public string? BeforeJson { get; init; }

    public string? AfterJson { get; init; }

    public string? ChangesJson { get; init; }
}
