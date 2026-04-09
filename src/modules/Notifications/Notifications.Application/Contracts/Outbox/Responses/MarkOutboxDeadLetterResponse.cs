namespace Notifications.Application.Contracts.Outbox.Responses;

public sealed class MarkOutboxDeadLetterResponse
{
    public long OutboxMessageId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}