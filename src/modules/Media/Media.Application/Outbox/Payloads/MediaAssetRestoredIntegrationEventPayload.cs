namespace Media.Application.Outbox.Payloads;

public sealed record MediaAssetRestoredIntegrationEventPayload(
    long MediaId,
    string MediaPublicId,
    bool IsDeleted,
    long ActorUserId,
    long Version,
    DateTime RestoredAtUtc,
    string BusinessDedupeKey);