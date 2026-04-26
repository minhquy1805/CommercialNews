namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;

public sealed class MarkOutboxPublishedRequest
{
    public long OutboxMessageId { get; init; }
}