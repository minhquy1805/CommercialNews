namespace Seo.Application.Contracts.SeoMetadata.Responses;

public sealed class GetSeoMetadataListResponse
{
    public long SeoId { get; init; }

    public long ArticleId { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }

    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }

    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? TwitterImageUrl { get; init; }

    public int Version { get; init; }

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedByUserId { get; init; }
}