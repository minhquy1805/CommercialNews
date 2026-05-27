namespace Interaction.Application.Models.Results;

public sealed record PublicCommentItemResult(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    string Content,
    DateTime CreatedAtUtc);