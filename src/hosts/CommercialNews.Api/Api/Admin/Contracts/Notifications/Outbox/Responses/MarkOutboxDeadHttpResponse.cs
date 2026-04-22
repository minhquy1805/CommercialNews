namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.Outbox.Responses;

public sealed class MarkOutboxDeadHttpResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}