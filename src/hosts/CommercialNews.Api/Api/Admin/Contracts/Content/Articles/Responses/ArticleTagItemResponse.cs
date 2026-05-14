namespace CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;

public sealed class ArticleTagItemResponse
{
    public long ArticleId { get; init; }

    public long TagId { get; init; }

    public DateTime AttachedAt { get; init; }

    public long? AttachedByUserId { get; init; }
}