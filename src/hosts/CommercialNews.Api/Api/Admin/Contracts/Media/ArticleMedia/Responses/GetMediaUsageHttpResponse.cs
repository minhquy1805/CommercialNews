namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class GetMediaUsageHttpResponse
{
    public long MediaId { get; init; }

    public IReadOnlyCollection<GetMediaUsageItemHttpResponse> Items { get; init; }
        = Array.Empty<GetMediaUsageItemHttpResponse>();
}