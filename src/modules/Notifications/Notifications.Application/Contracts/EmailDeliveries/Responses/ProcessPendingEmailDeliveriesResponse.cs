namespace Notifications.Application.Contracts.EmailDeliveries.Responses;

public sealed class ProcessPendingEmailDeliveriesResponse
{
    public int ClaimedCount { get; init; }

    public int ProcessedCount { get; init; }

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }

    public int AmbiguousCount { get; init; }

    public int DeadCount { get; init; }

    public int SuppressedCount { get; init; }
}