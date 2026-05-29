namespace Interaction.Application.Contracts.Comments.HideComment;

public sealed class HideCommentResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long Version { get; init; }
}