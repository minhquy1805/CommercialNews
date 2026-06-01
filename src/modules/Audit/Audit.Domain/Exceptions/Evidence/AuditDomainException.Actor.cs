namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException ActorTypeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorTypeRequired,
            "Audit actor type is required.");
    }

    public static AuditDomainException ActorTypeInvalid(string actorType)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorTypeInvalid,
            $"Audit actor type '{actorType}' is invalid.");
    }

    public static AuditDomainException ActorUserIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorUserIdInvalidLength,
            "Audit actor user id must be 26 characters.");
    }

    public static AuditDomainException ActorEmailTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorEmailTooLong,
            "Audit actor email is too long.");
    }

    public static AuditDomainException ActorDisplayNameTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorDisplayNameTooLong,
            "Audit actor display name is too long.");
    }
}