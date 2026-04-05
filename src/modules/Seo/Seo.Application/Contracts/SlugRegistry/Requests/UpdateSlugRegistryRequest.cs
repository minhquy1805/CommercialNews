namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class UpdateSlugRegistryRequest
{
    public long SlugId { get; init; }

    public string Slug { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }
    public bool IsActive { get; init; }

    public long? UpdatedByUserId { get; init; }

    public int ExpectedVersion { get; init; }
}