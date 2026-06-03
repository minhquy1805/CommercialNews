namespace Audit.Application.Abstractions.Persistence.Results;

public sealed record AuditDashboardSummaryDataResult(
    DateTime? FromUtc,
    DateTime? ToUtc,
    int AuditEvents,
    int HighRiskEvents,
    int CriticalEvents,
    int FailedIngestion,
    int DuplicateIngestion,
    IReadOnlyList<AuditCountByValueResult> CountsByModule,
    IReadOnlyList<AuditCountByValueResult> CountsBySeverity,
    IReadOnlyList<AuditCountByValueResult> CountsByRiskLevel,
    DateTime GeneratedAtUtc,
    int? OldestFailedIngestionAgeSeconds);
