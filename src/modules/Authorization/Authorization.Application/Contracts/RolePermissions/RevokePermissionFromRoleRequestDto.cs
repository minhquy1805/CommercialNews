namespace Authorization.Application.Contracts.RolePermissions;

public sealed class RevokePermissionFromRoleRequestDto
{
    public long RoleId { get; init; }
    public long PermissionId { get; init; }
}