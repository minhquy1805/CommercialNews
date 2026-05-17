namespace Content.Application.Outbox.Payloads;

public sealed record ArticlePublishedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string FromStatus,
    string ToStatus,
    string? Slug,
    string? CanonicalUrl,
    string? Title,
    string? Summary,
    string? CoverImageUrl,
    long ActorUserId,
    long Version,
    DateTime PublishedAtUtc,
    string BusinessDedupeKey);