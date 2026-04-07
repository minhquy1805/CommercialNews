namespace Interaction.Application.Contracts.Comments.Responses;

public sealed class CreateCommentResponse
{
    public long CommentId { get; init; }

    public DateTime CreatedAt { get; init; }
}