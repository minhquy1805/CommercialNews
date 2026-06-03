namespace Audit.Application.Abstractions.Persistence.Queries;

public sealed record AuditDashboardSummarySearchQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? SourceModule);