namespace Reading.Application.Models.QueryModels;

public sealed class ReadingArticleDetailResult
{
    public long ArticleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public ReadingCategoryResultItem? Category { get; init; }

    public IReadOnlyList<ReadingTagResultItem> Tags { get; init; } = Array.Empty<ReadingTagResultItem>();

    public IReadOnlyList<ReadingArticleMediaResultItem> Media { get; init; } = Array.Empty<ReadingArticleMediaResultItem>();

    public ReadingArticleSeoResult? Seo { get; init; }

    public ReadingArticleCountersResult? Counters { get; init; }
}