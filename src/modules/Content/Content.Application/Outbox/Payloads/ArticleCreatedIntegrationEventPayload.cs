namespace Content.Application.Outbox.Payloads;

public sealed record ArticleCreatedIntegrationEventPayload(
    long ArticleId,
    string ArticlePublicId,
    long CategoryId,
    long AuthorUserId,
    long CreatedByUserId,
    string Status,
    IReadOnlyCollection<long> TagIds,
    long Version,
    DateTime CreatedAtUtc,
    string BusinessDedupeKey);