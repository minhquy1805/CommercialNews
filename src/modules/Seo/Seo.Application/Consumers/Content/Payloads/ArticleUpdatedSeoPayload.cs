namespace Seo.Application.Consumers.Content.Payloads;

public sealed class ArticleUpdatedSeoPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? CoverImageUrl { get; init; }

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
