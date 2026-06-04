namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException ActionRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActionRequired,
            "Audit action is required.");
    }

    public static AuditDomainException ActionTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActionTooLong,
            "Audit action is too long.");
    }

    public static AuditDomainException ActionCategoryInvalid(string actionCategory)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActionCategoryInvalid,
            $"Audit action category '{actionCategory}' is invalid.");
    }

    public static AuditDomainException ActionCategoryTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActionCategoryTooLong,
            "Audit action category is too long.");
    }
}