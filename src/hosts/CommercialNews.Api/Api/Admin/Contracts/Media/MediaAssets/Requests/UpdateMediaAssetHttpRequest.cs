namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Requests;

public sealed class UpdateMediaAssetHttpRequest
{
    public string? AltText { get; init; }

    public string? MetadataJson { get; init; }
}