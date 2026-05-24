namespace Seo.Application.Consumers.Content.Payloads;

public sealed class ArticleCreatedSeoPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? CoverImageUrl { get; init; }

    public long CreatedByUserId { get; init; }

    public long Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
