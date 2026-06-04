namespace Audit.Application.Models.Results.Dashboard;

public sealed record AuditDashboardWindowResult(
    DateTime? FromUtc,
    DateTime? ToUtc);