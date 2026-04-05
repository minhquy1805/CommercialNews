namespace Seo.Application.Contracts.SlugRegistry.Requests;

public sealed class ActivateSlugRegistryRequest
{
    public long SlugId { get; init; }

    public long? UpdatedByUserId { get; init; }

    public int ExpectedVersion { get; init; }
}