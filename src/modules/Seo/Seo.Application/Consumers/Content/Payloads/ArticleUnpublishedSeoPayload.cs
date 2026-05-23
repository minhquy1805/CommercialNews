namespace Seo.Application.Consumers.Content.Payloads;

public sealed class ArticleUnpublishedSeoPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime UnpublishedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
