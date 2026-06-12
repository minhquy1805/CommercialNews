namespace Content.Application.Outbox.Payloads;

public sealed record ArticleUpdatedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string Status,
    long CategoryId,
    string? CategoryName,
    long AuthorUserId,
    long ActorUserId,
    long RevisionId,
    string? ChangeSummary,
    string? Slug,
    string? CanonicalUrl,
    string? Title,
    string? Summary,
    string? Body,
    long? CoverMediaId,
    string? CoverImageUrl,
    IReadOnlyCollection<long> TagIds,
    IReadOnlyCollection<ArticleTagIntegrationEventPayload> Tags,
    long Version,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);
