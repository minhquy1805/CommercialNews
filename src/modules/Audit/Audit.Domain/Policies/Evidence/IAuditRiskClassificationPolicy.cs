namespace Audit.Domain.Policies.Evidence;

public interface IAuditRiskClassificationPolicy
{
    AuditRiskClassificationResult Classify(
        string sourceModule,
        string eventType,
        string action,
        string? actionCategory);
}
