using Audit.Domain.Constants.AuditIngestion;

namespace Audit.Application.Models.Results.Ingestion;

public sealed record IngestAuditEventResult(
    string MessageId,
    string Status,
    long? AuditIngestionId,
    long? AuditLogId,
    bool WasInserted,
    string? Reason)
{
    public static IngestAuditEventResult Inserted(
        string messageId,
        long auditIngestionId,
        long auditLogId)
    {
        return new IngestAuditEventResult(
            MessageId: messageId,
            Status: AuditIngestionStatuses.Succeeded,
            AuditIngestionId: auditIngestionId,
            AuditLogId: auditLogId,
            WasInserted: true,
            Reason: null);
    }

    public static IngestAuditEventResult AlreadyProcessed(
        string messageId,
        long? auditIngestionId,
        long? auditLogId,
        string currentStatus,
        string? reason = null)
    {
        return new IngestAuditEventResult(
            MessageId: messageId,
            Status: currentStatus,
            AuditIngestionId: auditIngestionId,
            AuditLogId: auditLogId,
            WasInserted: false,
            Reason: reason);
    }

    public static IngestAuditEventResult Duplicate(
        string messageId,
        long? auditIngestionId,
        long? auditLogId,
        string? reason = null)
    {
        return new IngestAuditEventResult(
            MessageId: messageId,
            Status: AuditIngestionStatuses.Duplicate,
            AuditIngestionId: auditIngestionId,
            AuditLogId: auditLogId,
            WasInserted: false,
            Reason: reason);
    }

    public static IngestAuditEventResult Ignored(
        string messageId,
        long? auditIngestionId,
        string? reason)
    {
        return new IngestAuditEventResult(
            MessageId: messageId,
            Status: AuditIngestionStatuses.Ignored,
            AuditIngestionId: auditIngestionId,
            AuditLogId: null,
            WasInserted: false,
            Reason: reason);
    }

    public static IngestAuditEventResult Failed(
        string messageId,
        long? auditIngestionId,
        string? reason)
    {
        return new IngestAuditEventResult(
            MessageId: messageId,
            Status: AuditIngestionStatuses.Failed,
            AuditIngestionId: auditIngestionId,
            AuditLogId: null,
            WasInserted: false,
            Reason: reason);
    }

    public static IngestAuditEventResult DeadLettered(
        string messageId,
        long? auditIngestionId,
        string? reason)
    {
        return new IngestAuditEventResult(
            MessageId: messageId,
            Status: AuditIngestionStatuses.DeadLettered,
            AuditIngestionId: auditIngestionId,
            AuditLogId: null,
            WasInserted: false,
            Reason: reason);
    }
}