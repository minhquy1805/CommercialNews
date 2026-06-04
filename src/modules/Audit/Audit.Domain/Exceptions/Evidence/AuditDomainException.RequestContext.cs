namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException IpAddressTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.IpAddressTooLong,
            "Audit IP address is too long.");
    }

    public static AuditDomainException UserAgentTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.UserAgentTooLong,
            "Audit user agent is too long.");
    }
}