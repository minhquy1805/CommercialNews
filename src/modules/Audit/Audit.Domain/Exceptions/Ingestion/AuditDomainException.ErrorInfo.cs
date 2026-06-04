namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException ConsumerNameRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ConsumerNameRequired,
            "Audit consumer name is required.");
    }

    public static AuditDomainException ConsumerNameTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ConsumerNameTooLong,
            "Audit consumer name is too long.");
    }

    public static AuditDomainException IngestionStatusRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.IngestionStatusRequired,
            "Audit ingestion status is required.");
    }

    public static AuditDomainException IngestionStatusInvalid(string status)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.IngestionStatusInvalid,
            $"Audit ingestion status '{status}' is invalid.");
    }

    public static AuditDomainException InvalidStatusTransition(
        string currentStatus,
        string nextStatus,
        string? reason = null)
    {
        var message = string.IsNullOrWhiteSpace(reason)
            ? $"Audit ingestion status transition from '{currentStatus}' to '{nextStatus}' is invalid."
            : $"Audit ingestion status transition from '{currentStatus}' to '{nextStatus}' is invalid. Reason: {reason}";

        return new AuditDomainException(
            AuditDomainErrorCodes.IngestionStatusTransitionInvalid,
            message);
    }

    public static AuditDomainException AttemptCountInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AttemptCountInvalid,
            "Audit ingestion attempt count is invalid.");
    }

    public static AuditDomainException ErrorCodeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ErrorCodeTooLong,
            "Audit error code is too long.");
    }

    public static AuditDomainException ErrorMessageTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ErrorMessageTooLong,
            "Audit error message is too long.");
    }

    public static AuditDomainException ErrorClassInvalid(string errorClass)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ErrorClassInvalid,
            $"Audit error class '{errorClass}' is invalid.");
    }
}