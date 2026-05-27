namespace Interaction.Application.Contracts.Comments.DeleteOwnComment;

public sealed class DeleteOwnCommentRequestDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    /// <summary>
    /// Optional optimistic concurrency version.
    /// When supplied, it must be greater than or equal to one.
    /// </summary>
    public long? ExpectedVersion { get; init; }
}