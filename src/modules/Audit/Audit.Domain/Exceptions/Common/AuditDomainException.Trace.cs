namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException CorrelationIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.CorrelationIdTooLong,
            "Audit correlation id is too long.");
    }

    public static AuditDomainException CausationIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.CausationIdTooLong,
            "Audit causation id is too long.");
    }

    public static AuditDomainException TraceIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.TraceIdTooLong,
            "Audit trace id is too long.");
    }
}