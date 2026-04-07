namespace Interaction.Application.Contracts.Likes.Requests;

public sealed class UnlikeArticleRequest
{
    public long ArticleId { get; init; }

    public long UserId { get; init; }
}