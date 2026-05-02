namespace Audit.Application.Contracts.Ingestion;

public sealed class AuditIngestionResult
{
    public long AuditId { get; init; }

    public bool WasInserted { get; init; }

    public bool WasDeduped => !WasInserted;

    public static AuditIngestionResult Inserted(long auditId)
    {
        return new AuditIngestionResult
        {
            AuditId = auditId,
            WasInserted = true
        };
    }

    public static AuditIngestionResult Deduped(long auditId)
    {
        return new AuditIngestionResult
        {
            AuditId = auditId,
            WasInserted = false
        };
    }
}