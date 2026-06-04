namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException SummaryRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SummaryRequired,
            "Audit summary is required.");
    }

    public static AuditDomainException SummaryTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SummaryTooLong,
            "Audit summary is too long.");
    }
}