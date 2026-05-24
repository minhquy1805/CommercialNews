namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class ArticleDetailHttpResponse
{
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

    public DateTime? PublishedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public ArticleCountersHttpResponse Counters { get; init; } = new();

    public double? PopularityScore { get; init; }

    public IReadOnlyList<ArticleTagHttpResponse> Tags { get; init; } = [];

    public IReadOnlyList<ArticleMediaHttpResponse> Media { get; init; } = [];
}
