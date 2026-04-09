namespace Notifications.Application.Contracts.Services;

public sealed class NotificationDedupeCheckRequest
{
    public string MessageId { get; init; } = string.Empty;

    public string BusinessDedupeKey { get; init; } = string.Empty;
}