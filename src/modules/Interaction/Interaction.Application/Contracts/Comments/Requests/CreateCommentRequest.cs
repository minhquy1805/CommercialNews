namespace Interaction.Application.Contracts.Comments.Requests;

public sealed class CreateCommentRequest
{
    public long ArticleId { get; init; }

    public long UserId { get; init; }

    public long? ParentCommentId { get; init; }

    public string Content { get; init; } = string.Empty;
}