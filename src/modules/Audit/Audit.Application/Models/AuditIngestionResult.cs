namespace Audit.Application.Models;

public sealed class AuditIngestionResult
{
    public long? AuditId { get; init; }

    public string AuditEventId { get; init; } = string.Empty;

    public string Outcome { get; init; } = AuditIngestionOutcome.Failed;
}