namespace Notifications.Application.Contracts.Services;

public sealed class DedupeCheckResult
{
    public bool IsDuplicateMessage { get; init; }

    public bool IsDuplicateBusinessIntent { get; init; }

    public bool ShouldSuppress { get; init; }

    public string? Reason { get; init; }
}