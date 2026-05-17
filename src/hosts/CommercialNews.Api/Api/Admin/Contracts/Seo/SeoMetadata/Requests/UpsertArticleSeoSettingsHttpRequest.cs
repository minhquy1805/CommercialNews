namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SeoMetadata.Requests;

public sealed class UpsertArticleSeoSettingsHttpRequest
{
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

    public string? Robots { get; init; }

    public bool IsIndexable { get; init; } = true;
    public bool IsActive { get; init; } = true;

    public int? ExpectedSlugVersion { get; init; }
    public int? ExpectedSeoMetadataVersion { get; init; }
}
