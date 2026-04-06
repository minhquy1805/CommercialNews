namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Requests;

public sealed class UpdateSlugRouteHttpRequest
{
    public string Slug { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public string? CanonicalUrl { get; init; }

    public bool IsIndexable { get; init; }
    public bool IsActive { get; init; }

    public int ExpectedVersion { get; init; }
}