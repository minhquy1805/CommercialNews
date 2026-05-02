namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record PermissionDeactivatedAuditPayload(
    long PermissionId,
    string PermissionPublicId,
    string PermissionKey,
    string? PermissionModule,
    string? PermissionAction,
    bool PermissionIsSystem,
    long? DeactivatedByUserId,
    DateTime DeactivatedAtUtc,
    string BusinessDedupeKey);