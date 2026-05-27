namespace Interaction.Application.Contracts.Comments.RestoreComment;

public sealed class RestoreCommentResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long Version { get; init; }
}