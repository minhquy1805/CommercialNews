namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardCountByModuleResult(
    string SourceModule,
    int Count);