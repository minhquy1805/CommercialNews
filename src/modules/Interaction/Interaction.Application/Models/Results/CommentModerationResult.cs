namespace Interaction.Application.Models.Results;

public sealed record CommentModerationResult(
    string CommentPublicId,
    string ArticlePublicId,
    string Status,
    long Version,
    DateTime UpdatedAtUtc);