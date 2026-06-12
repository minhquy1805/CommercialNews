namespace Reading.Application.Consumers.Content.Payloads;

public sealed class ArticleUpdatedReadingPayload
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

    public string? Summary { get; init; }

    public string? Body { get; init; }

    public long? CoverMediaId { get; init; }

    public string? CoverImageUrl { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public IReadOnlyCollection<ArticleTagReadingPayload>? Tags { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
