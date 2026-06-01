namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException ResourceTypeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceTypeRequired,
            "Audit resource type is required.");
    }

    public static AuditDomainException ResourceTypeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceTypeTooLong,
            "Audit resource type is too long.");
    }

    public static AuditDomainException ResourceIdRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceIdRequired,
            "Audit resource id is required.");
    }

    public static AuditDomainException ResourceIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceIdTooLong,
            "Audit resource id is too long.");
    }

    public static AuditDomainException ResourceDisplayNameTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceDisplayNameTooLong,
            "Audit resource display name is too long.");
    }
}