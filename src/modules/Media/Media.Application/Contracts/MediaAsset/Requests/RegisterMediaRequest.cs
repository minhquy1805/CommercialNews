namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class RegisterMediaRequest
{
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
    public byte[]? ContentHash { get; init; }

    public long? ActorUserId { get; init; }
}