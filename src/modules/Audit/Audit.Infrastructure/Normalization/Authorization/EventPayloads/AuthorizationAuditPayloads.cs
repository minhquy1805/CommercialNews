namespace Audit.Infrastructure.Normalization.Authorization.EventPayloads;

internal sealed class UserRoleAssignedAuditPayload
{
    public long UserId { get; init; }
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public bool RoleIsSystem { get; init; }
    public long? AssignedByUserId { get; init; }
    public DateTime AssignedAtUtc { get; init; }
}

internal sealed class UserRoleRevokedAuditPayload
{
    public long UserId { get; init; }
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public bool RoleIsSystem { get; init; }
    public long? RevokedByUserId { get; init; }
    public DateTime RevokedAtUtc { get; init; }
}

internal sealed class RolePermissionGrantedAuditPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public bool RoleIsSystem { get; init; }
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string? PermissionModule { get; init; }
    public string? PermissionAction { get; init; }
    public bool PermissionIsSystem { get; init; }
    public long? GrantedByUserId { get; init; }
    public DateTime GrantedAtUtc { get; init; }
}

internal sealed class RolePermissionRevokedAuditPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public bool RoleIsSystem { get; init; }
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string? PermissionModule { get; init; }
    public string? PermissionAction { get; init; }
    public bool PermissionIsSystem { get; init; }
    public long? RevokedByUserId { get; init; }
    public DateTime RevokedAtUtc { get; init; }
}

internal sealed class RoleCreatedAuditPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string RoleNameNormalized { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public string? RoleDescription { get; init; }
    public bool RoleIsSystem { get; init; }
    public bool RoleIsActive { get; init; }
    public long? CreatedByUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

internal sealed class RoleUpdatedAuditPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string RoleNameNormalized { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public string? RoleDescription { get; init; }
    public bool RoleIsSystem { get; init; }
    public bool RoleIsActive { get; init; }
    public long? UpdatedByUserId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

internal sealed class RoleActivatedAuditPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public bool RoleIsSystem { get; init; }
    public long? ActivatedByUserId { get; init; }
    public DateTime ActivatedAtUtc { get; init; }
}

internal sealed class RoleDeactivatedAuditPayload
{
    public long RoleId { get; init; }
    public string RolePublicId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string? RoleDisplayName { get; init; }
    public bool RoleIsSystem { get; init; }
    public long? DeactivatedByUserId { get; init; }
    public DateTime DeactivatedAtUtc { get; init; }
}

internal sealed class PermissionCreatedAuditPayload
{
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string PermissionKeyNormalized { get; init; } = string.Empty;
    public string? PermissionModule { get; init; }
    public string? PermissionAction { get; init; }
    public string? PermissionDescription { get; init; }
    public bool PermissionIsSystem { get; init; }
    public bool PermissionIsActive { get; init; }
    public long? CreatedByUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

internal sealed class PermissionUpdatedAuditPayload
{
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string PermissionKeyNormalized { get; init; } = string.Empty;
    public string? PermissionModule { get; init; }
    public string? PermissionAction { get; init; }
    public string? PermissionDescription { get; init; }
    public bool PermissionIsSystem { get; init; }
    public bool PermissionIsActive { get; init; }
    public long? UpdatedByUserId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

internal sealed class PermissionActivatedAuditPayload
{
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string? PermissionModule { get; init; }
    public string? PermissionAction { get; init; }
    public bool PermissionIsSystem { get; init; }
    public long? ActivatedByUserId { get; init; }
    public DateTime ActivatedAtUtc { get; init; }
}

internal sealed class PermissionDeactivatedAuditPayload
{
    public long PermissionId { get; init; }
    public string PermissionPublicId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
    public string? PermissionModule { get; init; }
    public string? PermissionAction { get; init; }
    public bool PermissionIsSystem { get; init; }
    public long? DeactivatedByUserId { get; init; }
    public DateTime DeactivatedAtUtc { get; init; }
}
