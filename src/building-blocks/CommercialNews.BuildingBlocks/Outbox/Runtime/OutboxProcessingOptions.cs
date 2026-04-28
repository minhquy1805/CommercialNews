namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public sealed class OutboxProcessingOptions
{
    public int MaxRetryAttempts { get; init; } = 5;

    public int InitialRetryDelaySeconds { get; init; } = 30;

    public int MaxRetryDelaySeconds { get; init; } = 900;
}