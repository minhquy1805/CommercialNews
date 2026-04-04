namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class GetMediaByPublicIdRequest
{
    public string PublicId { get; init; } = string.Empty;
}