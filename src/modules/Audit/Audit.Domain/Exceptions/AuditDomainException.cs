using CommercialNews.BuildingBlocks.Domain.Exceptions;

namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException : DomainException
{
    private AuditDomainException(string code, string message)
        : base(code, message)
    {
    }

    private AuditDomainException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }
}