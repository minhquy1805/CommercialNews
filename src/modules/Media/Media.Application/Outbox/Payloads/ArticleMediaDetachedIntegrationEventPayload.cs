namespace Media.Application.Outbox.Payloads;

public sealed record ArticleMediaDetachedIntegrationEventPayload(
    long ArticleId,
    long MediaId,
    bool PrimaryCleared,
    long ActorUserId,
    long AttachmentSetVersion,
    DateTime DetachedAtUtc,
    string BusinessDedupeKey);