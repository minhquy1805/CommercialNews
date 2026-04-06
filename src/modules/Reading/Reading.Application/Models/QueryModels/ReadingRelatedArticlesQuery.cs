namespace Reading.Application.Models.QueryModels;

public sealed class ReadingRelatedArticlesQuery
{
    public long ArticleId { get; init; }

    public int Limit { get; init; } = 6;
}