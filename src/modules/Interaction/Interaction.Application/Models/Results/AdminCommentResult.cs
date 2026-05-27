namespace Interaction.Application.Models.Results;

public sealed record AdminCommentResult(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    string Content,
    string Status,
    string? ParentCommentPublicId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? DeletedAtUtc,
    long Version);