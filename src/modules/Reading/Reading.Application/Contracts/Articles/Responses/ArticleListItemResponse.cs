namespace Reading.Application.Contracts.Articles.Responses;

public sealed class ArticleListItemResponse
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public long? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public long? AuthorUserId { get; init; }

    public string? AuthorDisplayName { get; init; }

    public long? CoverMediaId { get; init; }

    public string? CoverMediaUrl { get; init; }

    public string? CoverAlt { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public long ViewCount { get; init; }

    public long LikeCount { get; init; }

    public long VisibleCommentCount { get; init; }
}
