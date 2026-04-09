namespace Notifications.Application.Contracts.Outbox.Requests;

public sealed class GetOutboxMessageByIdRequest
{
    public long OutboxMessageId { get; init; }
}