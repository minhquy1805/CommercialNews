namespace Media.Application.Contracts.MediaAsset.Responses;

public sealed class GetMediaListResponse
{
    public IReadOnlyCollection<GetMediaListItemResponse> Items { get; init; } =
        Array.Empty<GetMediaListItemResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}