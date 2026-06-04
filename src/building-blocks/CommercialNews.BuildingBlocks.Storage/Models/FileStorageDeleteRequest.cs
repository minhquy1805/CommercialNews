namespace CommercialNews.BuildingBlocks.Storage.Models;

public sealed class FileStorageDeleteRequest
{
    public string StorageProvider { get; init; } = string.Empty;

    public string StoragePath { get; init; } = string.Empty;
}