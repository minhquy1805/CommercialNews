namespace Reading.Application.Contracts.Responses;

public sealed class ArticleListItemResponse
{
    public long ArticleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public CategorySummaryResponse? Category { get; set; }

    public MediaSummaryResponse? Cover { get; set; }

    public ArticleCountersResponse? Counters { get; set; }
}