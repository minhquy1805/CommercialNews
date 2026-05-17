namespace Content.Application.Outbox.Payloads;

public sealed record ArticleUpdatedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    string Status,
    long CategoryId,
    long ActorUserId,
    long RevisionId,
    string? ChangeSummary,
    string? Slug,
    string? CanonicalUrl,
    string? Title,
    string? Summary,
    string? CoverImageUrl,
    IReadOnlyCollection<long> TagIds,
    long Version,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);