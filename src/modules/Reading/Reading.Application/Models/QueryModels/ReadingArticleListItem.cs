namespace Reading.Application.Models.QueryModels;

public sealed class ReadingArticleListItem
{
    public long ArticleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public ReadingCategoryResultItem? Category { get; init; }

    public ReadingArticleMediaResultItem? Cover { get; init; }

    public ReadingArticleCountersResult? Counters { get; init; }
}