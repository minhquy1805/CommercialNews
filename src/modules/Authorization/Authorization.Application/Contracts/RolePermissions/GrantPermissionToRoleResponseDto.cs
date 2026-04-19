namespace Authorization.Application.Contracts.RolePermissions;

public sealed class GrantPermissionToRoleResponseDto
{
    public long RoleId { get; init; }
    public long PermissionId { get; init; }

    public bool IsGranted { get; init; }
    public bool WasAlreadyGranted { get; init; }
}