namespace Content.Application.Outbox.Payloads;

public sealed record ArticleCreatedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    long CategoryId,
    string? CategoryName,
    long AuthorUserId,
    long CreatedByUserId,
    string Status,
    string? Slug,
    string? CanonicalUrl,
    string? Title,
    string? Summary,
    string? Body,
    long? CoverMediaId,
    string? CoverImageUrl,
    IReadOnlyCollection<long> TagIds,
    long Version,
    DateTime CreatedAtUtc,
    string BusinessDedupeKey);
