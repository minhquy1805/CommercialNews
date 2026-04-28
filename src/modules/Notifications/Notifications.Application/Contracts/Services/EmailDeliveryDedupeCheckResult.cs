namespace Notifications.Application.Contracts.Services;

public sealed class EmailDeliveryDedupeCheckResult
{
    public bool IsDuplicateMessage { get; init; }

    public bool IsDuplicateBusinessIntent { get; init; }

    public bool ShouldSuppress { get; init; }

    public long? ExistingEmailDeliveryId { get; init; }

    public string? ExistingStatus { get; init; }

    public string? Reason { get; init; }

    public bool HasDuplicate => IsDuplicateMessage || IsDuplicateBusinessIntent;
}