namespace CommercialNews.Worker.Notifications.Processing;

public sealed class EmailDeliveryProcessingWorkerOptions
{
    public const string SectionName = "Workers:EmailDeliveryProcessing";

    public bool IsEnabled { get; init; } = true;

    public int BatchSize { get; init; } = 50;

    public int PollIntervalSeconds { get; init; } = 10;

    public int BusyDelaySeconds { get; init; } = 1;

    public int ErrorDelaySeconds { get; init; } = 5;
}