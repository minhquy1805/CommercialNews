namespace Seo.Application.Contracts.SlugRegistry.Responses;

public sealed class ResolveByScopeAndSlugResponse
{
    public string Scope { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }

    public string Status { get; init; } = string.Empty;

    public long? SourceAggregateVersion { get; init; }

    public int Version { get; init; }
}