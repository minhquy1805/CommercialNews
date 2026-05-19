namespace CommercialNews.BuildingBlocks.Storage.Models;

public sealed class FileStorageUploadResult
{
    public string StorageProvider { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string StoragePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long FileSizeBytes { get; init; }

    public byte[] ContentHash { get; init; } = Array.Empty<byte>();
}