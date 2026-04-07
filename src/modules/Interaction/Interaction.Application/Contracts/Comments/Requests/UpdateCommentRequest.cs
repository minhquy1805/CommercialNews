namespace Interaction.Application.Contracts.Comments.Requests;

public sealed class UpdateCommentRequest
{
    public long CommentId { get; init; }

    public long UserId { get; init; }

    public string Content { get; init; } = string.Empty;
}