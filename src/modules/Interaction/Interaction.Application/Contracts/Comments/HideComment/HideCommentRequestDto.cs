namespace Interaction.Application.Contracts.Comments.HideComment;

public sealed class HideCommentRequestDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public long ExpectedVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Note { get; init; }
}