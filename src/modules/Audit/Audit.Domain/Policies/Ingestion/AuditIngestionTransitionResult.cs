namespace Audit.Domain.Policies.Ingestion;

public sealed record AuditIngestionTransitionResult
{
    public bool IsAllowed { get; }
    public string? ErrorCode { get; }
    public string? Reason { get; }

    private AuditIngestionTransitionResult(
        bool isAllowed,
        string? errorCode,
        string? reason)
    {
        IsAllowed = isAllowed;
        ErrorCode = errorCode;
        Reason = reason;
    }

    public static AuditIngestionTransitionResult Allowed()
    {
        return new AuditIngestionTransitionResult(
            isAllowed: true,
            errorCode: null,
            reason: null);
    }

    public static AuditIngestionTransitionResult Denied(
        string errorCode,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Transition denial error code is required.", nameof(errorCode));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Transition denial reason is required.", nameof(reason));
        }

        return new AuditIngestionTransitionResult(
            isAllowed: false,
            errorCode: errorCode.Trim(),
            reason: reason.Trim());
    }
}