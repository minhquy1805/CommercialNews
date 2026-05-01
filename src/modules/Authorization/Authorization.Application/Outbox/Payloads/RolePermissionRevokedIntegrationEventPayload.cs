namespace Authorization.Application.Outbox.Payloads;

public sealed record RolePermissionRevokedIntegrationEventPayload(
    long RoleId,
    string RolePublicId,
    string RoleName,
    string? RoleDisplayName,
    bool RoleIsSystem,
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string? PermissionModule,
    string? PermissionAction,
    bool PermissionIsSystem,
    long? RevokedByUserId,
    DateTime RevokedAtUtc,
    string BusinessDedupeKey);