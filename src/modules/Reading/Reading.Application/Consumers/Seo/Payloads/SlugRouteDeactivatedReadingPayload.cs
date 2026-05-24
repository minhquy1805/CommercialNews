namespace Reading.Application.Consumers.Seo.Payloads;

public sealed class SlugRouteDeactivatedReadingPayload
{
    public string Scope { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsActive { get; init; }

    public bool IsIndexable { get; init; }

    public long? ActorUserId { get; init; }

    /// <summary>
    /// Version of SlugRegistry / SlugRoute aggregate.
    /// This is used as SourceVersion for ArticleSeoRouteProjection only.
    /// </summary>
    public long Version { get; init; }

    public DateTime DeactivatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}