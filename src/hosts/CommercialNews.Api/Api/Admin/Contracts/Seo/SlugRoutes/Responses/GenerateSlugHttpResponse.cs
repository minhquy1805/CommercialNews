namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;

public sealed class GenerateSlugHttpResponse
{
    public string Scope { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string SuggestedSlug { get; init; } = string.Empty;

    public bool IsUnique { get; init; }
}