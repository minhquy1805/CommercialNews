namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;

public sealed class GetOutboxMessageByIdRequest
{
    public long OutboxMessageId { get; init; }
}