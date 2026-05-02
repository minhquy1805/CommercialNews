namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record PermissionUpdatedAuditPayload(
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