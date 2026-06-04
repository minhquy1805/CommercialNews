namespace Audit.Application.Abstractions.Persistence.Commands;

public sealed record AuditUnsupportedIngestionUpsertCommand(
    string PublicId,
    string MessageId,
    string EventType,
    string AggregateType,
    string AggregateId,
    string? AggregatePublicId,
    int? AggregateVersion,
    string? CorrelationId,
    int Priority,
    DateTime OccurredAtUtc,
    DateTime? PublishedAtUtc,
    string ConsumerName,
    DateTime NowUtc);