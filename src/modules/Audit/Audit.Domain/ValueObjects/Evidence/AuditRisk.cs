using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Evidence;

public sealed record AuditRisk
{
    public string Outcome { get; }
    public string Severity { get; }
    public string RiskLevel { get; }

    private AuditRisk(
        string outcome,
        string severity,
        string riskLevel)
    {
        Outcome = outcome;
        Severity = severity;
        RiskLevel = riskLevel;
    }

    public static AuditRisk Create(
        string? outcome,
        string? severity,
        string? riskLevel)
    {
        var normalizedOutcome = outcome?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedOutcome))
        {
            throw AuditDomainException.OutcomeRequired();
        }

        if (!AuditOutcomes.IsValid(normalizedOutcome))
        {
            throw AuditDomainException.OutcomeInvalid(normalizedOutcome);
        }

        var normalizedSeverity = severity?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSeverity))
        {
            throw AuditDomainException.SeverityRequired();
        }

        if (!AuditSeverities.IsValid(normalizedSeverity))
        {
            throw AuditDomainException.SeverityInvalid(normalizedSeverity);
        }

        var normalizedRiskLevel = riskLevel?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRiskLevel))
        {
            throw AuditDomainException.RiskLevelRequired();
        }

        if (!AuditRiskLevels.IsValid(normalizedRiskLevel))
        {
            throw AuditDomainException.RiskLevelInvalid(normalizedRiskLevel);
        }

        return new AuditRisk(
            normalizedOutcome,
            normalizedSeverity,
            normalizedRiskLevel);
    }

    public static AuditRisk InfoLowSuccess()
    {
        return Create(
            outcome: AuditOutcomes.Success,
            severity: AuditSeverities.Info,
            riskLevel: AuditRiskLevels.Low);
    }

    public static AuditRisk WarningCriticalSuccess()
    {
        return Create(
            outcome: AuditOutcomes.Success,
            severity: AuditSeverities.Warning,
            riskLevel: AuditRiskLevels.Critical);
    }

    public static AuditRisk FailedErrorHigh()
    {
        return Create(
            outcome: AuditOutcomes.Failure,
            severity: AuditSeverities.Error,
            riskLevel: AuditRiskLevels.High);
    }
}