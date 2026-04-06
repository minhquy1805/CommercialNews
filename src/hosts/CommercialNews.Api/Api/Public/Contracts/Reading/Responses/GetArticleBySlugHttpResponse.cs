namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class GetArticleBySlugHttpResponse
{
    public long ArticleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public CategorySummaryHttpResponse? Category { get; init; }

    public IReadOnlyList<TagSummaryHttpResponse> Tags { get; init; } = Array.Empty<TagSummaryHttpResponse>();

    public IReadOnlyList<MediaSummaryHttpResponse> Media { get; init; } = Array.Empty<MediaSummaryHttpResponse>();

    public SeoSummaryHttpResponse? Seo { get; init; }

    public ArticleCountersHttpResponse? Counters { get; init; }
}