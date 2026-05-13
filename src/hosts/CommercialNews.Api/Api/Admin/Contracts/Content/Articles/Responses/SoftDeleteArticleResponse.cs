namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;

public sealed class SoftDeleteArticleResponse
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public bool IsDeleted { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public long? DeletedByUserId { get; init; }
}