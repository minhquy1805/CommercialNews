namespace Interaction.Application.Contracts.Likes.UnlikeArticle;

public sealed class UnlikeArticleRequestDto
{
    public string ArticlePublicId { get; init; } = string.Empty;
}