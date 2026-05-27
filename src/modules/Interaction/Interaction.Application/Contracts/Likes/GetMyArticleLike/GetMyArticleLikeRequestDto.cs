namespace Interaction.Application.Contracts.Likes.GetMyArticleLike;

public sealed class GetMyArticleLikeRequestDto
{
    public string ArticlePublicId { get; init; } = string.Empty;
}