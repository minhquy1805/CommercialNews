namespace Interaction.Application.Models.Results;

public sealed record DeleteOwnCommentMutationResult(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    string Status,
    long Version,
    DateTime? DeletedAtUtc,
    bool Changed,
    bool ClosedOpenCase,
    bool WasVisible);