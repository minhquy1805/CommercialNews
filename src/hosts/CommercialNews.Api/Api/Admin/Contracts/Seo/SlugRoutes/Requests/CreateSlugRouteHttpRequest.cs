namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Requests;

public sealed class CreateSlugRouteHttpRequest
{
    public long ArticleId { get; init; }

    public string Slug { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }
    public bool IsActive { get; init; } = true;
}