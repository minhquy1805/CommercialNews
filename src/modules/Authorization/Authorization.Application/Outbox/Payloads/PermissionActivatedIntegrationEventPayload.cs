namespace Authorization.Application.Outbox.Payloads;

public sealed record PermissionActivatedIntegrationEventPayload(
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string? PermissionModule,
    string? PermissionAction,
    bool PermissionIsSystem,
    long? ActivatedByUserId,
    DateTime ActivatedAtUtc,
    string BusinessDedupeKey);