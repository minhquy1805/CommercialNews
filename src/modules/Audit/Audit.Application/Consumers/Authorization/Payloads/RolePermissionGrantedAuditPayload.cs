namespace Audit.Application.Consumers.Authorization.Payloads;

public sealed record RolePermissionGrantedAuditPayload(
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
    long? GrantedByUserId,
    DateTime GrantedAtUtc,
    string BusinessDedupeKey);