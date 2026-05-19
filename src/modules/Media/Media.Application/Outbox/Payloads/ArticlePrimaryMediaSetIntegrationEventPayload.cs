namespace Media.Application.Outbox.Payloads;

public sealed record ArticlePrimaryMediaSetIntegrationEventPayload(
    long ArticleId,
    long MediaId,
    long ActorUserId,
    long AttachmentSetVersion,
    DateTime PrimarySetAtUtc,
    string BusinessDedupeKey);