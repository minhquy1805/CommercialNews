namespace Interaction.Application.Models.Results;

public sealed record ModerationCaseCommentDetailResult(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    string Content,
    string Status,
    long Version,
    DateTime CreatedAtUtc);