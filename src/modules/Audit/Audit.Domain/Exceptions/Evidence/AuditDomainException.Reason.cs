namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException ReasonTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ReasonTooLong,
            "Audit reason is too long.");
    }
}
