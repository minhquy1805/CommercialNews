namespace Authorization.Application.Outbox.Payloads;

public sealed record PermissionUpdatedIntegrationEventPayload(
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string PermissionKeyNormalized,
    string? PermissionModule,
    string? PermissionAction,
    string? PermissionDescription,
    bool PermissionIsSystem,
    bool PermissionIsActive,
    long? UpdatedByUserId,
    DateTime UpdatedAtUtc,
    string BusinessDedupeKey);