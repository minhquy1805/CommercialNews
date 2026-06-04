namespace Audit.Domain.Policies.Ingestion;

public interface IAuditIngestionTransitionPolicy
{
    AuditIngestionTransitionResult Evaluate(
        string currentStatus,
        string nextStatus);

    bool IsTerminal(string status);
}