namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentDeletedByAuthorPayload(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    bool WasVisible,
    bool ClosedOpenCase,
    DateTime DeletedAtUtc);