namespace CommercialNews.Worker.Configuration;

public sealed class OutboxWorkerOptions
{
    public bool IsEnabled { get; init; } = true;

    public int BatchSize { get; init; } = 50;

    public int PollIntervalSeconds { get; init; } = 10;

    public int ErrorDelaySeconds { get; init; } = 5;

    public bool StopOnFirstFailure { get; init; } = false;

    public int BusyDelaySeconds { get; init; } = 1;

    public int MaxRetryAttempts { get; init; } = 5;
}