namespace Reading.Application.Consumers.Seo.Payloads;

public sealed class SeoMetadataUpdatedReadingPayload
{
    public string Scope { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public string? MetaTitle { get; init; }

    public string? MetaDescription { get; init; }

    public string? OgTitle { get; init; }

    public string? OgDescription { get; init; }

    public string? OgImageUrl { get; init; }

    public string? TwitterTitle { get; init; }

    public string? TwitterDescription { get; init; }

    public string? TwitterImageUrl { get; init; }

    public string? Robots { get; init; }

    public bool IsManualOverride { get; init; }

    public long? ActorUserId { get; init; }

    /// <summary>
    /// Version of SeoMetadata aggregate.
    /// This is used as SourceVersion for ArticleSeoMetadataProjection only.
    /// </summary>
    public long Version { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}