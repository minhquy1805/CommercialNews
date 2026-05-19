namespace Media.Application.Outbox.Payloads;

public sealed record MediaAssetSoftDeletedIntegrationEventPayload(
    long MediaId,
    string MediaPublicId,
    bool IsDeleted,
    DateTime? RestoreUntil,
    int PrimaryClearedCount,
    long ActorUserId,
    long Version,
    DateTime DeletedAtUtc,
    string BusinessDedupeKey);