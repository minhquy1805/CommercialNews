namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException JsonPayloadInvalid(Exception innerException)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.JsonPayloadInvalid,
            "Audit JSON payload is invalid.",
            innerException);
    }
}