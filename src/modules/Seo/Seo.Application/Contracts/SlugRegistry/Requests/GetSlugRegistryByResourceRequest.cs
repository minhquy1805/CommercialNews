namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class GetSlugRegistryByResourceRequest
{
    public string? Scope { get; init; }

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;

    public bool? OnlyActive { get; init; }
}