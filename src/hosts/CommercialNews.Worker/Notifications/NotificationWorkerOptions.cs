namespace CommercialNews.Worker.Notifications;

public sealed class NotificationWorkerOptions
{
    public bool IsEnabled { get; init; } = true;

    public int BatchSize { get; init; } = 20;

    public int PollIntervalSeconds { get; init; } = 5;

    public int ErrorDelaySeconds { get; init; } = 10;
}