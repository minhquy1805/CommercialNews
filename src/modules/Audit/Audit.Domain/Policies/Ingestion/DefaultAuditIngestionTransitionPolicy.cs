using Audit.Domain.Constants.AuditIngestion;

namespace Audit.Domain.Policies.Ingestion;

public sealed class DefaultAuditIngestionTransitionPolicy : IAuditIngestionTransitionPolicy
{
    private const string InvalidCurrentStatusCode = "AUDIT_INGESTION_INVALID_CURRENT_STATUS";
    private const string InvalidNextStatusCode = "AUDIT_INGESTION_INVALID_NEXT_STATUS";
    private const string TerminalStatusCode = "AUDIT_INGESTION_TERMINAL_STATUS";
    private const string InvalidTransitionCode = "AUDIT_INGESTION_INVALID_TRANSITION";

    public AuditIngestionTransitionResult Evaluate(
        string currentStatus,
        string nextStatus)
    {
        var normalizedCurrentStatus = currentStatus?.Trim();
        var normalizedNextStatus = nextStatus?.Trim();

        if (!AuditIngestionStatuses.IsValid(normalizedCurrentStatus))
        {
            return AuditIngestionTransitionResult.Denied(
                InvalidCurrentStatusCode,
                $"Audit ingestion current status '{normalizedCurrentStatus}' is invalid.");
        }

        if (!AuditIngestionStatuses.IsValid(normalizedNextStatus))
        {
            return AuditIngestionTransitionResult.Denied(
                InvalidNextStatusCode,
                $"Audit ingestion next status '{normalizedNextStatus}' is invalid.");
        }

        if (AuditIngestionStatuses.IsTerminal(normalizedCurrentStatus))
        {
            return AuditIngestionTransitionResult.Denied(
                TerminalStatusCode,
                $"Audit ingestion status '{normalizedCurrentStatus}' is terminal.");
        }

        if (IsAllowedTransition(normalizedCurrentStatus!, normalizedNextStatus!))
        {
            return AuditIngestionTransitionResult.Allowed();
        }

        return AuditIngestionTransitionDenied(normalizedCurrentStatus!, normalizedNextStatus!);
    }

    public bool IsTerminal(string status)
    {
        return AuditIngestionStatuses.IsTerminal(status?.Trim());
    }

    private static bool IsAllowedTransition(
        string currentStatus,
        string nextStatus)
    {
        if (EqualsStatus(currentStatus, AuditIngestionStatuses.Processing))
        {
            return EqualsStatus(nextStatus, AuditIngestionStatuses.Processing) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.Succeeded) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.Duplicate) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.Ignored) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.Failed) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.DeadLettered);
        }

        if (EqualsStatus(currentStatus, AuditIngestionStatuses.Failed))
        {
            return EqualsStatus(nextStatus, AuditIngestionStatuses.Processing) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.Failed) ||
                   EqualsStatus(nextStatus, AuditIngestionStatuses.DeadLettered);
        }

        return false;
    }

    private static AuditIngestionTransitionResult AuditIngestionTransitionDenied(
        string currentStatus,
        string nextStatus)
    {
        return AuditIngestionTransitionResult.Denied(
            InvalidTransitionCode,
            $"Audit ingestion status transition from '{currentStatus}' to '{nextStatus}' is invalid.");
    }

    private static bool EqualsStatus(
        string left,
        string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
