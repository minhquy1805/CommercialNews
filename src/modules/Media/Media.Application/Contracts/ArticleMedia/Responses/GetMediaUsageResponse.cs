namespace Media.Application.Contracts.ArticleMedia.Responses;

public sealed class GetMediaUsageResponse
{
    public long MediaId { get; init; }

    public IReadOnlyCollection<GetMediaUsageItemResponse> Items { get; init; }
        = Array.Empty<GetMediaUsageItemResponse>();
}