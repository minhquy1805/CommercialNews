namespace CommercialNews.Api.Api.Admin.Contracts.Seo.SeoMetadata.Responses;

public sealed class GetSeoMetadataListHttpResponse
{
    public IReadOnlyCollection<GetSeoMetadataListItemHttpResponse> Items { get; init; }
        = Array.Empty<GetSeoMetadataListItemHttpResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}