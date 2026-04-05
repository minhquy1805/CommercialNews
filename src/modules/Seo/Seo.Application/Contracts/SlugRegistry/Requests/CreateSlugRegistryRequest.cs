namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class CreateSlugRegistryRequest
{
    public long ArticleId { get; init; }

    public string Slug { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }
    public bool IsActive { get; init; } = true;

    public long? CreatedByUserId { get; init; }
}