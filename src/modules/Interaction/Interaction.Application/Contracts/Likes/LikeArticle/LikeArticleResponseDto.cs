namespace Interaction.Application.Contracts.Likes.LikeArticle;

public sealed class LikeArticleResponseDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public bool Liked { get; init; }

    public long Version { get; init; }
}