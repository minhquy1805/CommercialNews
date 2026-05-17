namespace Content.Application.Outbox.Payloads;

public sealed record ArticleCreatedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    long CategoryId,
    long AuthorUserId,
    long CreatedByUserId,
    string Status,
    string? Slug,
    string? CanonicalUrl,
    string? Title,
    string? Summary,
    string? CoverImageUrl,
    IReadOnlyCollection<long> TagIds,
    long Version,
    DateTime CreatedAtUtc,
    string BusinessDedupeKey);