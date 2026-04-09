namespace Notifications.Application.Contracts.Outbox.Requests;

public sealed class ProcessOutboxMessageRequest
{
    public long OutboxMessageId { get; init; }
}