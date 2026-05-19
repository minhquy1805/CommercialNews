namespace Audit.Application.Consumers.Media.Payloads;

public sealed class MediaAssetRegisteredAuditPayload
{
    public long MediaId { get; init; }

    public string MediaPublicId { get; init; } = string.Empty;

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

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime RegisteredAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}