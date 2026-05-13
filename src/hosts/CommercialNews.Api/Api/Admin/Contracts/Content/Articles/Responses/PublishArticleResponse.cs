namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;

public sealed class PublishArticleResponse
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? PublishedAt { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }
}