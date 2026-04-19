namespace Authorization.Application.Contracts.UserRoles;

public sealed class RevokeRoleFromUserResponseDto
{
    public long UserId { get; init; }
    public long RoleId { get; init; }

    public bool IsRevoked { get; init; }
    public bool WasAlreadyRevoked { get; init; }
}