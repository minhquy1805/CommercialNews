namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException AggregateTypeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregateTypeTooLong,
            "Audit aggregate type is too long.");
    }

    public static AuditDomainException AggregateIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregateIdTooLong,
            "Audit aggregate id is too long.");
    }

    public static AuditDomainException AggregatePublicIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregatePublicIdInvalidLength,
            "Audit aggregate public id must be 26 characters.");
    }

    public static AuditDomainException AggregateVersionInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregateVersionInvalid,
            "Audit aggregate version must be greater than or equal to 1.");
    }
}