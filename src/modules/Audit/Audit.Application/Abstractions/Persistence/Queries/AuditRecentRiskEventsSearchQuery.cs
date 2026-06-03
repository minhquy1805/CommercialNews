namespace Audit.Application.Abstractions.Persistence.Queries;

public sealed record AuditRecentRiskEventsSearchQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? SourceModule,
    string? RiskLevel,
    int Limit);