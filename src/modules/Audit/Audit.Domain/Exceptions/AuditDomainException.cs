using CommercialNews.BuildingBlocks.Domain.Exceptions;

namespace Audit.Domain.Exceptions;

public sealed class AuditDomainException : DomainException
{
    private AuditDomainException(string code, string message)
        : base(code, message)
    {
    }

    private AuditDomainException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }

    public static AuditDomainException JsonPayloadInvalid(Exception innerException)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.JsonPayloadInvalid,
            "Audit json payload is invalid.",
            innerException);
    }

    public static AuditDomainException MessageIdRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.MessageIdRequired,
            "Audit message id is required.");
    }
}