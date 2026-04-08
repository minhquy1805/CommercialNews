namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Likes.Requests;

public sealed class UnlikeArticleHttpRequest
{
    public long ArticleId { get; init; }
}