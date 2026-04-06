namespace Reading.Application.Models.QueryModels;

public sealed class ReadingArticleDetailResult
{
    public long ArticleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public ReadingCategoryResultItem? Category { get; set; }

    public IReadOnlyList<ReadingTagResultItem> Tags { get; set; } = Array.Empty<ReadingTagResultItem>();

    public IReadOnlyList<ReadingArticleMediaResultItem> Media { get; set; } = Array.Empty<ReadingArticleMediaResultItem>();

    public ReadingArticleSeoResult? Seo { get; set; }

    public ReadingArticleCountersResult? Counters { get; set; }
}