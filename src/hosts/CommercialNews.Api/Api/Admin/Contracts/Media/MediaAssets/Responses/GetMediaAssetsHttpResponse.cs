namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class GetMediaAssetsHttpResponse
{
    public IReadOnlyCollection<GetMediaAssetsItemHttpResponse> Items { get; init; }
        = Array.Empty<GetMediaAssetsItemHttpResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}