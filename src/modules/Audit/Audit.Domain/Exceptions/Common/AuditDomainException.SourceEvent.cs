namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException PublicIdRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.PublicIdRequired,
            "Audit public id is required.");
    }

    public static AuditDomainException PublicIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.PublicIdInvalidLength,
            "Audit public id must be 26 characters.");
    }

    public static AuditDomainException MessageIdRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.MessageIdRequired,
            "Audit message id is required.");
    }

    public static AuditDomainException MessageIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.MessageIdInvalidLength,
            "Audit message id must be 26 characters.");
    }

    public static AuditDomainException EventTypeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.EventTypeRequired,
            "Audit event type is required.");
    }

    public static AuditDomainException EventTypeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.EventTypeTooLong,
            "Audit event type is too long.");
    }

    public static AuditDomainException SourceModuleRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceModuleRequired,
            "Audit source module is required.");
    }

    public static AuditDomainException SourceModuleInvalid(string sourceModule)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceModuleInvalid,
            $"Audit source module '{sourceModule}' is invalid.");
    }

    public static AuditDomainException SourceModuleTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceModuleTooLong,
            "Audit source module is too long.");
    }

    public static AuditDomainException EventVersionInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.EventVersionInvalid,
            "Audit event version must be greater than or equal to 1.");
    }

    public static AuditDomainException SourcePriorityInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourcePriorityInvalid,
            "Audit source priority is invalid.");
    }

    public static AuditDomainException SourceOccurredAtUtcRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceOccurredAtUtcRequired,
            "Audit source occurred time is required.");
    }

    public static AuditDomainException SourcePublishedAtUtcInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourcePublishedAtUtcInvalid,
            "Audit source published time is invalid.");
    }
}
