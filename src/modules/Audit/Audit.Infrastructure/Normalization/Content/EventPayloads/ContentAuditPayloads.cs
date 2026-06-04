namespace Audit.Infrastructure.Normalization.Content.EventPayloads;

internal sealed class ArticleCreatedAuditPayload
{
    public long ArticleId { get; init; }
    public string ArticlePublicId { get; init; } = string.Empty;
    public long CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public long AuthorUserId { get; init; }
    public long CreatedByUserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? Title { get; init; }
    public long? CoverMediaId { get; init; }
    public string? CoverImageUrl { get; init; }
    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();
    public long Version { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

internal sealed class ArticleUpdatedAuditPayload
{
    public long ArticleId { get; init; }
    public string ArticlePublicId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public long AuthorUserId { get; init; }
    public long ActorUserId { get; init; }
    public long RevisionId { get; init; }
    public string? ChangeSummary { get; init; }
    public string? Slug { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? Title { get; init; }
    public long? CoverMediaId { get; init; }
    public string? CoverImageUrl { get; init; }
    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();
    public long Version { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

internal sealed class ArticlePublishedAuditPayload
{
    public long ArticleId { get; init; }
    public string ArticlePublicId { get; init; } = string.Empty;
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public long CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public long AuthorUserId { get; init; }
    public string? Slug { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? Title { get; init; }
    public long? CoverMediaId { get; init; }
    public string? CoverImageUrl { get; init; }
    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime PublishedAtUtc { get; init; }
}

internal sealed class ArticleUnpublishedAuditPayload
{
    public long ArticleId { get; init; }
    public string ArticlePublicId { get; init; } = string.Empty;
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime UnpublishedAtUtc { get; init; }
}

internal sealed class ArticleArchivedAuditPayload
{
    public long ArticleId { get; init; }
    public string ArticlePublicId { get; init; } = string.Empty;
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime ArchivedAtUtc { get; init; }
}

internal sealed class ArticleSoftDeletedAuditPayload
{
    public long ArticleId { get; init; }
    public string ArticlePublicId { get; init; } = string.Empty;
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public long ActorUserId { get; init; }
    public long Version { get; init; }
    public DateTime DeletedAtUtc { get; init; }
}
