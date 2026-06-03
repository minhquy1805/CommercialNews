namespace Audit.Application.Abstractions.Persistence.Queries;

public sealed record AuditFailedIngestionSearchQuery(
    string? EventType,
    string? AggregateType,
    string? AggregateId,
    string? AggregatePublicId,
    string? CorrelationId,
    string? ConsumerName,
    string? LastErrorClass,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection);