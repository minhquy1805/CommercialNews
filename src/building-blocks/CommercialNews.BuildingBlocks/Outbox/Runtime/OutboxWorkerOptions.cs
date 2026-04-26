namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public sealed class OutboxWorkerOptions
{
    public bool IsEnabled { get; init; } = true;

    public int BatchSize { get; init; } = 50;

    public int PollIntervalSeconds { get; init; } = 10;

    public int ErrorDelaySeconds { get; init; } = 5;
}