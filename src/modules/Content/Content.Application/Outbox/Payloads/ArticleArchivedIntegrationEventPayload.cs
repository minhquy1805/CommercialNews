namespace Content.Application.Outbox.Payloads;

public sealed record ArticleArchivedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string FromStatus,
    string ToStatus,
    long ActorUserId,
    long Version,
    DateTime ArchivedAtUtc,
    string BusinessDedupeKey);