namespace Reading.Application.Contracts.Responses;

public sealed class GetArticleBySlugResponse
{
    public long ArticleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public CategorySummaryResponse? Category { get; set; }

    public IReadOnlyList<TagSummaryResponse> Tags { get; set; } = Array.Empty<TagSummaryResponse>();

    public IReadOnlyList<MediaSummaryResponse> Media { get; set; } = Array.Empty<MediaSummaryResponse>();

    public SeoSummaryResponse? Seo { get; set; }

    public ArticleCountersResponse? Counters { get; set; }
}