namespace Audit.Application.Abstractions.Persistence.Results;

public sealed record AuditLogInsertResult(
    long AuditLogId,
    bool WasInserted);