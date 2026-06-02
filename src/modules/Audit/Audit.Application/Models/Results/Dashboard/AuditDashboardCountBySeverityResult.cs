namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardCountBySeverityResult(
    string Severity,
    int Count);