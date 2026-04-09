namespace Notifications.Application.Contracts.Outbox.Requests;

public sealed class GetOutboxMessageByMessageIdRequest
{
    public string MessageId { get; init; } = string.Empty;
}