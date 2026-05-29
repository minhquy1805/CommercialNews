namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Likes.Responses;

public sealed class GetMyArticleLikeResponse
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public bool Liked { get; init; }

    public long? Version { get; init; }
}