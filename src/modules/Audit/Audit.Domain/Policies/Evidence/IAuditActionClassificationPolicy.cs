namespace Audit.Domain.Policies.Evidence;

public interface IAuditActionClassificationPolicy
{
    AuditActionClassificationResult Classify(
        string sourceModule,
        string eventType);
}