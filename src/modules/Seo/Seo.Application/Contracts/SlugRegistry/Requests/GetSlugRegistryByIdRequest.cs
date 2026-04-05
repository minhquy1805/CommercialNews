namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class GetSlugRegistryByIdRequest
{
    public long SlugId { get; init; }
}