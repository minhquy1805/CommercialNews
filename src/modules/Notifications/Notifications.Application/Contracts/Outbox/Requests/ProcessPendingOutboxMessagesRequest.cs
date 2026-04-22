namespace Notifications.Application.Contracts.Outbox.Requests;

public sealed class ProcessPendingOutboxMessagesRequest
{
    public int BatchSize { get; init; } = 20;

    public bool StopOnFirstFailure { get; init; } = false;
}