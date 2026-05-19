using Microsoft.AspNetCore.Http;

namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Requests;

public sealed class UploadMediaAssetHttpRequest
{
    public IFormFile File { get; init; } = default!;

    public string MediaType { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public string? MetadataJson { get; init; }

    public string? Folder { get; init; }
}