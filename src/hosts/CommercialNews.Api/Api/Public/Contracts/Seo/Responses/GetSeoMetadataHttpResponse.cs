namespace CommercialNews.Api.Api.Public.Contracts.Seo.Responses;

public sealed class GetSeoMetadataHttpResponse
{
    public string ResourceType { get; init; } = string.Empty;
    public long ResourceId { get; init; }

    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }

    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }

    public int Version { get; init; }
}