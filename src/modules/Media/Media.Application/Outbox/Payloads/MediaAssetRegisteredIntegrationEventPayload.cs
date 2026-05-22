namespace Media.Application.Outbox.Payloads;

public sealed record MediaAssetRegisteredIntegrationEventPayload(
    long MediaId,
    string MediaPublicId,
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
    long ActorUserId,
    long Version,
    DateTime RegisteredAtUtc,
    string BusinessDedupeKey);