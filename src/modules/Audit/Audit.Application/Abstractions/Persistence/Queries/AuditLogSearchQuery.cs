namespace Audit.Application.Abstractions.Persistence.Queries;

public sealed record AuditLogSearchQuery(
    string? SourceModule,
    string? EventType,
    string? Action,
    string? ActionCategory,
    string? ResourceType,
    string? ResourceId,
    string? ActorUserId,
    long? ActorInternalId,
    string? Outcome,
    string? Severity,
    string? RiskLevel,
    string? CorrelationId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection);