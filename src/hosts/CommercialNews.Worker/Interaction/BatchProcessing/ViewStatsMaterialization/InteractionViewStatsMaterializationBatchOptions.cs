namespace CommercialNews.Worker.Interaction.BatchProcessing.ViewStatsMaterialization;

public sealed class InteractionViewStatsMaterializationBatchOptions
{
    public const string SectionName =
        "Workers:Interaction:BatchProcessing:ViewStatsMaterialization";

    public bool IsEnabled { get; init; }

    public int BatchSize { get; init; }

    public int PollIntervalSeconds { get; init; }

    public int BusyDelaySeconds { get; init; }

    public int ErrorDelaySeconds { get; init; }
}