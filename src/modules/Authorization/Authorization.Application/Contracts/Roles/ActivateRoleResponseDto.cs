namespace Authorization.Application.Contracts.Roles;

public sealed class ActivateRoleResponseDto
{
    public long RoleId { get; init; }
    public bool IsActivated { get; init; }
    public bool WasAlreadyActivated { get; init; }
}