namespace Notifications.Application.Contracts.Outbox.Responses;

public sealed class ProcessPendingOutboxMessagesResponse
{
    public int RequestedBatchSize { get; init; }

    public int ClaimedCount { get; init; }

    public int ProcessedCount { get; init; }

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }

    public IReadOnlyList<ProcessPendingOutboxMessageItemResponse> Items { get; init; }
        = Array.Empty<ProcessPendingOutboxMessageItemResponse>();
}