namespace Authorization.Application.Contracts.UserRoles;

public sealed class AssignRoleToUserRequestDto
{
    public long UserId { get; init; }
    public long RoleId { get; init; }
}