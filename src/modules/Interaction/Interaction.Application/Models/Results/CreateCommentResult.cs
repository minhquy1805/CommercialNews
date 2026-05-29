namespace Interaction.Application.Models.Results;

public sealed record CreateCommentResult(
    string CommentPublicId,
    string ArticlePublicId,
    string Status,
    DateTime CreatedAtUtc,
    long Version);