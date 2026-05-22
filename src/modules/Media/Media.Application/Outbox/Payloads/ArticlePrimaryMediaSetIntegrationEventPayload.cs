namespace Media.Application.Outbox.Payloads;

public sealed record ArticlePrimaryMediaSetIntegrationEventPayload(
    long ArticleId,
    long MediaId,
    string MediaPublicId,
    string Url,
    string MediaType,
    string? AltText,
    string? AltTextOverride,
    string? EffectiveAltText,
    string? Caption,
    int SortOrder,
    long ActorUserId,
    long AttachmentSetVersion,
    DateTime PrimarySetAtUtc,
    string BusinessDedupeKey);
