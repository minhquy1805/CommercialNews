namespace Seo.Application.Contracts.SeoMetadata.Requests;

public sealed class GetSeoMetadataByResourceRequest
{
    public string? Scope { get; init; }

    public string ResourceType { get; init; } = string.Empty;

    public string ResourcePublicId { get; init; } = string.Empty;
}