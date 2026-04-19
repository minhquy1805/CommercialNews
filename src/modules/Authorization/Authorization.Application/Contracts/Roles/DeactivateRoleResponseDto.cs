namespace Authorization.Application.Contracts.Roles;

public sealed class DeactivateRoleResponseDto
{
    public long RoleId { get; init; }
    public bool IsDeactivated { get; init; }
    public bool WasAlreadyDeactivated { get; init; }
}