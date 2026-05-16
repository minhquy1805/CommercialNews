namespace Seo.Application.Models.Results;

public sealed class SeoMetadataListResultItem
{
    public long SeoId { get; init; }

    public string Scope { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

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

    public long? SourceAggregateVersion { get; init; }

    public string? LastAppliedMessageId { get; init; }

    public DateTime? LastSyncedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public long? UpdatedByUserId { get; init; }

    public int Version { get; init; }
}