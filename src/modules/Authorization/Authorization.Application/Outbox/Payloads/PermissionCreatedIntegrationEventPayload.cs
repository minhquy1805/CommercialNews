namespace Authorization.Application.Outbox.Payloads;

public sealed record PermissionCreatedIntegrationEventPayload(
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string PermissionKeyNormalized,
    string? PermissionModule,
    string? PermissionAction,
    string? PermissionDescription,
    bool PermissionIsSystem,
    bool PermissionIsActive,
    long? CreatedByUserId,
    DateTime CreatedAtUtc,
    string BusinessDedupeKey);