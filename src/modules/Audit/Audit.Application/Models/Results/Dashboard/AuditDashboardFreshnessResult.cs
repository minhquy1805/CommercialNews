namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardFreshnessResult(
    DateTime GeneratedAtUtc,
    int? OldestFailedIngestionAgeSeconds);