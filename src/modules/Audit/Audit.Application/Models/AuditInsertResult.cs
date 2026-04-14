namespace Audit.Application.Models;

public sealed class AuditInsertResult
{
    public long AuditId { get; init; }

    public bool WasInserted { get; init; }
}