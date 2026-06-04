namespace Audit.Application.Abstractions.Persistence.Results;

public sealed record AuditIngestionUpsertResult(
    long AuditIngestionId,
    bool WasInserted,
    string CurrentStatus);