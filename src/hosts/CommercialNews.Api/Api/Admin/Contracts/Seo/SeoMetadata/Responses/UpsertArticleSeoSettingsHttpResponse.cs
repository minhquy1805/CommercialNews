namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SeoMetadata.Responses;

public sealed class UpsertArticleSeoSettingsHttpResponse
{
    public bool Updated { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Scope { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string ResourcePublicId { get; init; } = string.Empty;
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

    public bool IsManualOverride { get; init; }

    public bool? IsIndexable { get; init; }
    public bool? IsActive { get; init; }

    public long? SourceAggregateVersion { get; init; }
    public string? LastAppliedMessageId { get; init; }
    public DateTime? LastSyncedAtUtc { get; init; }

    public int Version { get; init; }
}
