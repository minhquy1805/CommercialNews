namespace CommercialNews.BuildingBlocks.Storage.Models;

public sealed class FileStorageUploadRequest
{
    public Stream Content { get; init; } = Stream.Null;

    public string OriginalFileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long Length { get; init; }

    public string Purpose { get; init; } = string.Empty;

    public string? Folder { get; init; }

    public string? PreferredFileNameWithoutExtension { get; init; }
}