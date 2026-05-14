namespace Content.Application.Outbox.Payloads;

public sealed record ArticlePublishedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string FromStatus,
    string ToStatus,
    long ActorUserId,
    long Version,
    DateTime PublishedAtUtc,
    string BusinessDedupeKey);