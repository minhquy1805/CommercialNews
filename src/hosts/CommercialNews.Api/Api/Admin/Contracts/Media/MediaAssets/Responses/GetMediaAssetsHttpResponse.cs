namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class GetMediaAssetsHttpResponse
{
    public IReadOnlyCollection<GetMediaAssetsItemHttpResponse> Items { get; init; }
        = Array.Empty<GetMediaAssetsItemHttpResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}

public sealed class GetMediaAssetsItemHttpResponse
{
    public long MediaId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string StorageProvider { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? StoragePath { get; init; }
    public string? FileName { get; init; }
    public string MediaType { get; init; } = string.Empty;
    public string? MimeType { get; init; }
    public long? FileSizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }
    public string? AltText { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsDeleted { get; init; }
    public int Version { get; init; }
}