namespace Media.Application.Outbox.Payloads;

public sealed record ArticleMediaAttachedIntegrationEventPayload(
    long ArticleId,
    long MediaId,
    long? ArticleMediaId,
    bool IsPrimary,
    bool PrimaryChanged,
    long ActorUserId,
    long AttachmentSetVersion,
    DateTime AttachedAtUtc,
    string BusinessDedupeKey);