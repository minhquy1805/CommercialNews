namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record PermissionCreatedAuditPayload(
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