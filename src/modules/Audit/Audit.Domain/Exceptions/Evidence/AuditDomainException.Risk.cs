namespace Audit.Domain.Exceptions;

public sealed partial class AuditDomainException
{
    public static AuditDomainException OutcomeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.OutcomeRequired,
            "Audit outcome is required.");
    }

    public static AuditDomainException OutcomeInvalid(string outcome)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.OutcomeInvalid,
            $"Audit outcome '{outcome}' is invalid.");
    }

    public static AuditDomainException SeverityRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SeverityRequired,
            "Audit severity is required.");
    }

    public static AuditDomainException SeverityInvalid(string severity)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SeverityInvalid,
            $"Audit severity '{severity}' is invalid.");
    }

    public static AuditDomainException RiskLevelRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.RiskLevelRequired,
            "Audit risk level is required.");
    }

    public static AuditDomainException RiskLevelInvalid(string riskLevel)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.RiskLevelInvalid,
            $"Audit risk level '{riskLevel}' is invalid.");
    }
}