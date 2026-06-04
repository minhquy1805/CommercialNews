using CommercialNews.BuildingBlocks.Outbox.Runtime.Models;

namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;

public sealed class ProcessPendingOutboxMessagesResponse
{
    public int RequestedBatchSize { get; init; }

    public int ClaimedCount { get; init; }

    public int ProcessedCount { get; init; }

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }

    public IReadOnlyList<ProcessPendingOutboxMessageItemResult> Items { get; init; }
        = Array.Empty<ProcessPendingOutboxMessageItemResult>();
}