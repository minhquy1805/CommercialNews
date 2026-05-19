namespace Media.Application.Models.Commands;

public sealed record CreateMediaAssetCommand(
    string PublicId,
    string StorageProvider,
    string Url,
    string? StoragePath,
    string? FileName,
    string MediaType,
    string? MimeType,
    long? FileSizeBytes,
    int? Width,
    int? Height,
    int? DurationSeconds,
    string? AltText,
    string? MetadataJson,
    byte[]? ContentHash,
    long? CreatedBy);