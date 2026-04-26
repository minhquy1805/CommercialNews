namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;

public sealed class GetOutboxMessageByMessageIdRequest
{
    public string MessageId { get; init; } = string.Empty;
}