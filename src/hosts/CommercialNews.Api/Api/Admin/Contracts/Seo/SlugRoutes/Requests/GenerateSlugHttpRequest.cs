namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Requests;

public sealed class GenerateSlugHttpRequest
{
    public string Source { get; init; } = string.Empty;

    public string? Scope { get; init; }

    public string? ResourceType { get; init; }

    public string? ResourcePublicId { get; init; }
}
