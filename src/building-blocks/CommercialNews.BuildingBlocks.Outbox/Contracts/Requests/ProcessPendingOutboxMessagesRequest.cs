namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;

public sealed class ProcessPendingOutboxMessagesRequest
{
    public int BatchSize { get; init; } = 20;

    public bool StopOnFirstFailure { get; init; }
}