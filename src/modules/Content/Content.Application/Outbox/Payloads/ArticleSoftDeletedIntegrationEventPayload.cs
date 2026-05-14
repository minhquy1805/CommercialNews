namespace Content.Application.Outbox.Payloads;

public sealed record ArticleSoftDeletedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string FromStatus,
    string ToStatus,
    bool IsDeleted,
    long ActorUserId,
    long Version,
    DateTime DeletedAtUtc,
    string BusinessDedupeKey);