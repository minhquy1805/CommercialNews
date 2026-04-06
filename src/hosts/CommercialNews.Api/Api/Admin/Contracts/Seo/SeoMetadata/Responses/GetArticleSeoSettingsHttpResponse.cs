namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SeoMetadata.Responses;

public sealed class GetArticleSeoSettingsHttpResponse
{
    public long ArticleId { get; init; }

    public string? Scope { get; init; }
    public string? Slug { get; init; }

    public string? CanonicalUrl { get; init; }

    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }

    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgImageUrl { get; init; }

    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? TwitterImageUrl { get; init; }

    public bool? IsIndexable { get; init; }
    public bool? IsActive { get; init; }

    public int Version { get; init; }
}