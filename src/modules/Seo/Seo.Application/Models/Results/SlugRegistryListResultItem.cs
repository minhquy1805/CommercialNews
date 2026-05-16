namespace Seo.Application.Models.Results;

public sealed class SlugRegistryListResultItem
{
    public long SlugId { get; init; }

    public string Scope { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }

    public bool IsActive { get; init; }

    public long? SourceAggregateVersion { get; init; }

    public string? LastAppliedMessageId { get; init; }

    public DateTime? LastSyncedAtUtc { get; init; }

    public int Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public long? CreatedByUserId { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public long? UpdatedByUserId { get; init; }
}