namespace Audit.Domain.Policies.Redaction;

public interface IAuditRedactionPolicy
{
    AuditRedactionResult Redact(
        string sourceModule,
        string eventType,
        string? jsonPayload);

    AuditRedactionResult RedactHeaders(
        string sourceModule,
        string eventType,
        string? headersJson);

    AuditRedactionResult RedactMetadata(
        string sourceModule,
        string eventType,
        string? metadataJson);
}