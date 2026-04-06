namespace Reading.Application.Models.QueryModels;

public sealed class ReadingArticleCountersResult
{
    public long Views { get; init; }

    public long Likes { get; init; }

    public bool CountersPartial { get; init; }
}