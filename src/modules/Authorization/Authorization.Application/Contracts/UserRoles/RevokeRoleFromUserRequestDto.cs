namespace Authorization.Application.Contracts.UserRoles;

public sealed class RevokeRoleFromUserRequestDto
{
    public long UserId { get; init; }
    public long RoleId { get; init; }
}