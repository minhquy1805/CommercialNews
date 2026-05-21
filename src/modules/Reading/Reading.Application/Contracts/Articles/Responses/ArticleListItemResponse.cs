namespace Reading.Application.Contracts.Articles.Responses;

public sealed class ArticleListItemResponse
{
    public string ArticlePublicId { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public long? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public long? AuthorUserId { get; set; }

    public string? AuthorDisplayName { get; set; }

    public long? CoverMediaId { get; set; }

    public string? CoverMediaUrl { get; set; }

    public string? CoverAlt { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public long ViewCount { get; set; }

    public long LikeCount { get; set; }

    public long CommentCount { get; set; }

    public double? PopularityScore { get; set; }
}