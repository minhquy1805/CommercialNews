namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class GetMediaByIdRequest
{
    public long MediaId { get; init; }
}