namespace Authorization.Application.Contracts.RolePermissions;

public sealed class GrantPermissionToRoleRequestDto
{
    public long RoleId { get; init; }
    public long PermissionId { get; init; }
}