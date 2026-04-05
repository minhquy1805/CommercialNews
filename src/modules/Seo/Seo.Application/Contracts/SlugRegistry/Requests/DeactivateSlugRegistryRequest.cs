namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class DeactivateSlugRegistryRequest
{
    public long SlugId { get; init; }

    public long? UpdatedByUserId { get; init; }

    public int ExpectedVersion { get; init; }
}