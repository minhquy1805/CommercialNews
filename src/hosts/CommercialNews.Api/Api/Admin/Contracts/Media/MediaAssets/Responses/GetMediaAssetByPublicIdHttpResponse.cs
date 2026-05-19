namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class GetMediaAssetByPublicIdHttpResponse
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

    public string? MetadataJson { get; init; }

    public DateTime CreatedAt { get; init; }

    public long? CreatedBy { get; init; }

    public DateTime UpdatedAt { get; init; }

    public long? UpdatedBy { get; init; }

    public bool IsDeleted { get; init; }

    public DateTime? DeletedAt { get; init; }

    public long? DeletedBy { get; init; }

    public DateTime? RestoreUntil { get; init; }

    public DateTime? RestoredAt { get; init; }

    public long? RestoredBy { get; init; }

    public int Version { get; init; }
}