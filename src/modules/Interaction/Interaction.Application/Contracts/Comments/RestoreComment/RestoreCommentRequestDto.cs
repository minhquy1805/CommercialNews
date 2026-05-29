namespace Interaction.Application.Contracts.Comments.RestoreComment;

public sealed class RestoreCommentRequestDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public long ExpectedVersion { get; init; }

    public string? Note { get; init; }
}