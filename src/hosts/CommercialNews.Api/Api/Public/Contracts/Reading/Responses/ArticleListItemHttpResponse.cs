namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class ArticleListItemHttpResponse
{
    public long ArticleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public DateTime PublishedAt { get; init; }

    public CategorySummaryHttpResponse? Category { get; init; }

    public MediaSummaryHttpResponse? Cover { get; init; }

    public ArticleCountersHttpResponse? Counters { get; init; }
}