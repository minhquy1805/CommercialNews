namespace Notifications.Application.Contracts.Outbox.Requests;

public sealed class MarkOutboxPublishedRequest
{
    public long OutboxMessageId { get; init; }
}