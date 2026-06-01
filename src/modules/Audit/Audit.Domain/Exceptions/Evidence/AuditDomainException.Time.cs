namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException OccurredAtUtcRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.OccurredAtUtcRequired,
            "Audit occurred time is required.");
    }

    public static AuditDomainException IngestedAtUtcRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.IngestedAtUtcRequired,
            "Audit ingested time is required.");
    }

    public static AuditDomainException FirstReceivedAtUtcRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.FirstReceivedAtUtcRequired,
            "Audit first received time is required.");
    }

    public static AuditDomainException TimestampMustBeUtc(string parameterName)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.TimestampMustBeUtc,
            $"Audit timestamp '{parameterName}' must be UTC.");
    }

    public static AuditDomainException TimestampRequired(string parameterName)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.TimestampRequired,
            $"Audit timestamp '{parameterName}' is required.");
    }
}
