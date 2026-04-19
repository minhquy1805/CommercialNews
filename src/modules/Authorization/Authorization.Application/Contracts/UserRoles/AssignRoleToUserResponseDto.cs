namespace Authorization.Application.Contracts.UserRoles;

public sealed class AssignRoleToUserResponseDto
{
    public long UserId { get; init; }
    public long RoleId { get; init; }

    public bool IsAssigned { get; init; }
    public bool WasAlreadyAssigned { get; init; }
}