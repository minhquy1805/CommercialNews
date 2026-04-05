namespace Seo.Application.Contracts.SeoMetadata.Requests;

public sealed class UpdateSeoMetadataRequest
{
    public long SeoId { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }

    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }

    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? TwitterImageUrl { get; init; }

    public long? UpdatedByUserId { get; init; }

    public int ExpectedVersion { get; init; }
}