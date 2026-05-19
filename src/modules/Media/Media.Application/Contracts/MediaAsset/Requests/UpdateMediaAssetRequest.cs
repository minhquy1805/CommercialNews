namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class UpdateMediaAssetRequest
{
    public long MediaId { get; init; }

    public string? AltText { get; init; }
    public string? MetadataJson { get; init; }
}