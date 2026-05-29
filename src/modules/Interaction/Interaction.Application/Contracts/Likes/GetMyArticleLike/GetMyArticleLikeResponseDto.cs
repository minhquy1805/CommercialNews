namespace Interaction.Application.Contracts.Likes.GetMyArticleLike;

public sealed class GetMyArticleLikeResponseDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public bool Liked { get; init; }

    /// <summary>
    /// Current relationship version when an ArticleLike row exists.
    /// Null when the user has never liked this article.
    /// </summary>
    public long? Version { get; init; }
}