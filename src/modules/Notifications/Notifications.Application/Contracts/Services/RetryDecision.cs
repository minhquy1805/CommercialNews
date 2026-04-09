namespace Notifications.Application.Contracts.Services;

public sealed class RetryDecision
{
    public bool ShouldRetry { get; init; }

    public bool ShouldMarkDead { get; init; }

    public DateTime? NextRetryAt { get; init; }

    public string? Reason { get; init; }
}