namespace Reading.Application.Consumers.Content.Payloads;

public sealed class ArticlePublishedReadingPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string FromStatus { get; init; } = string.Empty;

    public string ToStatus { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public long AuthorUserId { get; init; }

    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? Body { get; init; }

    public long? CoverMediaId { get; init; }

    public string? CoverImageUrl { get; init; }

    public IReadOnlyCollection<long> TagIds { get; init; } = Array.Empty<long>();

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime PublishedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}