namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;

public sealed class GetSlugRoutesByArticleIdHttpResponse
{
    public IReadOnlyCollection<GetSlugRoutesByArticleIdItemHttpResponse> Items { get; init; }
        = Array.Empty<GetSlugRoutesByArticleIdItemHttpResponse>();
}