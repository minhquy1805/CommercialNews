namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;

public sealed class UpdateArticleResponse
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public long AuthorUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long? CoverMediaId { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }
}