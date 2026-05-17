namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;

public sealed class GenerateSlugHttpResponse
{
    public string Scope { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string SuggestedSlug { get; init; } = string.Empty;

    public bool IsUnique { get; init; }

    public string? ExistingResourceType { get; init; }

    public string? ExistingResourcePublicId { get; init; }
}
