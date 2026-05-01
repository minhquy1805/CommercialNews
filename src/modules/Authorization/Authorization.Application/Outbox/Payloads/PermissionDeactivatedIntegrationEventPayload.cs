namespace Authorization.Application.Outbox.Payloads;

public sealed record PermissionDeactivatedIntegrationEventPayload(
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string? PermissionModule,
    string? PermissionAction,
    bool PermissionIsSystem,
    long? DeactivatedByUserId,
    DateTime DeactivatedAtUtc,
    string BusinessDedupeKey);