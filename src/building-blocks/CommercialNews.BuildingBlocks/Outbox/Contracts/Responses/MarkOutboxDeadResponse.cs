namespace CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;

public sealed class MarkOutboxDeadResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}