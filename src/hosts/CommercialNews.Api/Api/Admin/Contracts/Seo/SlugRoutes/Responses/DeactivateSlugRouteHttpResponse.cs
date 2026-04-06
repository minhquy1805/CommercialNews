namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;

public sealed class DeactivateSlugRouteHttpResponse
{
    public long SlugId { get; init; }

    public long ArticleId { get; init; }

    public string Slug { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public bool IsActive { get; init; }
    public bool IsIndexable { get; init; }

    public int Version { get; init; }

    public DateTime UpdatedAt { get; init; }
    public long? UpdatedByUserId { get; init; }
}