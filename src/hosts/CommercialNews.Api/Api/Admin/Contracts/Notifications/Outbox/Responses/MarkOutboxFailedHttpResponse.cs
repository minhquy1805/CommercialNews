namespace CommercialNews.Api.Api.Admin.Contracts.Notifications.Outbox.Responses;

public sealed class MarkOutboxFailedHttpResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? NextRetryAt { get; init; }
}