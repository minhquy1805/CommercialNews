namespace Interaction.Application.Models.Results;

public sealed record ModerationCaseCommentResult(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    string Content,
    string Status,
    long Version);