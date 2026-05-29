namespace Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId;

public sealed class ModerationCaseCommentResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long Version { get; init; }
}