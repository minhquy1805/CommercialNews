namespace Interaction.Application.Contracts.Comments.Requests;

public sealed class DeleteCommentRequest
{
    public long CommentId { get; init; }

    public long UserId { get; init; }
}