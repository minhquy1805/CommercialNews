namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentCreatedPayload(
    string CommentPublicId,
    string ArticlePublicId,
    long AuthorUserId,
    string Status,
    DateTime CreatedAtUtc);