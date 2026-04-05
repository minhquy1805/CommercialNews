namespace Seo.Application.Contracts.SlugRegistry.Responses;

public sealed class GetSlugRegistryByIdResponse
{
    public long SlugId { get; init; }

    public long ArticleId { get; init; }

    public string Slug { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }
    public bool IsActive { get; init; }

    public int Version { get; init; }

    public DateTime CreatedAt { get; init; }
    public long? CreatedByUserId { get; init; }

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedByUserId { get; init; }
}