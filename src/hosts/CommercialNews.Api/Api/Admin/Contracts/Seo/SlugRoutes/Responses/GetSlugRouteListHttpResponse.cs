namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SlugRoutes.Responses;

public sealed class GetSlugRouteListHttpResponse
{
    public IReadOnlyCollection<GetSlugRouteListItemHttpResponse> Items { get; init; }
        = Array.Empty<GetSlugRouteListItemHttpResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}