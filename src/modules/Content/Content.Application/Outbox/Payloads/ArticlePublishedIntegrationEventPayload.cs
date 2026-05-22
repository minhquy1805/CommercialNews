namespace Content.Application.Outbox.Payloads;

public sealed record ArticlePublishedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string FromStatus,
    string ToStatus,
    long CategoryId,
    string? CategoryName,
    long AuthorUserId,
    string? Slug,
    string? CanonicalUrl,
    string? Title,
    string? Summary,
    string? Body,
    long? CoverMediaId,
    string? CoverImageUrl,
    IReadOnlyCollection<long> TagIds,
    long ActorUserId,
    long Version,
    DateTime PublishedAtUtc,
    string BusinessDedupeKey);
