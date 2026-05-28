namespace Reading.Application.Models.Results;

public sealed class ArticleDetailResult
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public long? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public long? AuthorUserId { get; init; }

    public string? AuthorDisplayName { get; init; }

    public long? CoverMediaId { get; init; }

    public string? CoverMediaUrl { get; init; }

    public string? CoverAlt { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? MetaTitle { get; init; }

    public string? MetaDescription { get; init; }

    public string? OgTitle { get; init; }

    public string? OgDescription { get; init; }

    public string? OgImageUrl { get; init; }

    public string? TwitterTitle { get; init; }

    public string? TwitterDescription { get; init; }

    public string? TwitterImageUrl { get; init; }

    public string? Robots { get; init; }

    public bool SeoIsManualOverride { get; init; }

    public bool SeoRouteIsActive { get; init; }

    public bool SeoIsIndexable { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public long ViewCount { get; init; }

    public long LikeCount { get; init; }

    public long VisibleCommentCount { get; init; }

    public IReadOnlyList<ArticleTagResult> Tags { get; init; } = [];

    public IReadOnlyList<ArticleMediaResult> Media { get; init; } = [];
}
