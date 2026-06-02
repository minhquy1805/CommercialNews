namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardTotalsResult(
    int AuditEvents,
    int HighRiskEvents,
    int CriticalEvents,
    int FailedIngestions,
    int DuplicateIngestions);