namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentRestoredPayload(
    string CommentPublicId,
    string ArticlePublicId,
    long ModeratorUserId,
    DateTime RestoredAtUtc);