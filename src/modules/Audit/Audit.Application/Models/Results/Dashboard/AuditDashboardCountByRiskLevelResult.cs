namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardCountByRiskLevelResult(
    string RiskLevel,
    int Count);