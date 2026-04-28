namespace Notifications.Application.Contracts.Services;

public sealed class RetryDecision
{
    public bool ShouldRetry { get; init; }

    public bool ShouldMarkDead { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public string? Reason { get; init; }

    public static RetryDecision RetryAt(DateTime nextRetryAt, string? reason = null)
    {
        return new RetryDecision
        {
            ShouldRetry = true,
            ShouldMarkDead = false,
            NextRetryAt = nextRetryAt,
            Reason = reason
        };
    }

    public static RetryDecision MarkDead(string? reason = null)
    {
        return new RetryDecision
        {
            ShouldRetry = false,
            ShouldMarkDead = true,
            NextRetryAt = null,
            Reason = reason
        };
    }

    public static RetryDecision NoRetry(string? reason = null)
    {
        return new RetryDecision
        {
            ShouldRetry = false,
            ShouldMarkDead = false,
            NextRetryAt = null,
            Reason = reason
        };
    }
}