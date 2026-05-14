namespace Content.Application.Outbox.Payloads;

public sealed record ArticleUnpublishedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string FromStatus,
    string ToStatus,
    string Reason,
    long ActorUserId,
    long Version,
    DateTime UnpublishedAtUtc,
    string BusinessDedupeKey);