namespace Notifications.Infrastructure.Services;

public sealed class NotificationEmailOptions
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public bool UseSsl { get; init; } = false;

    public string FromName { get; init; } = string.Empty;

    public string FromEmail { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public int TimeoutMilliseconds { get; init; } = 10000;
}