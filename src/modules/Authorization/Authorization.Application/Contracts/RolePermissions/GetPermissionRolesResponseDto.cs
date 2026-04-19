namespace Authorization.Application.Contracts.RolePermissions;

public sealed class GetPermissionRolesResponseDto
{
    public long PermissionId { get; init; }
    public IReadOnlyList<PermissionRoleItemDto> Roles { get; init; } = Array.Empty<PermissionRoleItemDto>();
}