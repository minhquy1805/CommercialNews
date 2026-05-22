namespace Media.Application.Outbox.Payloads;

public sealed record ArticleMediaAttachedIntegrationEventPayload(
    long ArticleId,
    long MediaId,
    string MediaPublicId,
    long? ArticleMediaId,
    string Url,
    string MediaType,
    string? AltText,
    string? AltTextOverride,
    string? EffectiveAltText,
    string? Caption,
    int SortOrder,
    bool IsPrimary,
    bool PrimaryChanged,
    long ActorUserId,
    long AttachmentSetVersion,
    DateTime AttachedAtUtc,
    string BusinessDedupeKey);
