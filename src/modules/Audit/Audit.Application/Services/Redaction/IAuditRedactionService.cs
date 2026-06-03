namespace Audit.Application.Services.Redaction;

public interface IAuditRedactionService
{
    AuditRedactionOutput Redact(
        AuditRedactionInput input);
}