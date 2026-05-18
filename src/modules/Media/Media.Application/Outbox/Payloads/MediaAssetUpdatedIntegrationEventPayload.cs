namespace Media.Application.Outbox.Payloads;

public sealed record MediaAssetUpdatedIntegrationEventPayload(
    long MediaId,
    string MediaPublicId,
    string? AltText,
    string? MetadataJson,
    long ActorUserId,
    long Version,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);