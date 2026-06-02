namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardSummaryResult(
    AuditDashboardWindowResult Window,
    AuditDashboardTotalsResult Totals,
    IReadOnlyList<AuditDashboardCountByModuleResult> ByModule,
    IReadOnlyList<AuditDashboardCountBySeverityResult> BySeverity,
    IReadOnlyList<AuditDashboardCountByRiskLevelResult> ByRiskLevel,
    AuditDashboardFreshnessResult Freshness);