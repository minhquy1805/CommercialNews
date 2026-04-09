namespace Notifications.Application.Contracts.Outbox.Requests;

public sealed class MarkOutboxDeadLetterRequest
{
    public long OutboxMessageId { get; init; }

    public string? LastError { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorClass { get; init; }
}