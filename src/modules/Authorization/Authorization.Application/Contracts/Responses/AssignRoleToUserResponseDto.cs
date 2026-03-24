namespace Authorization.Application.Contracts.Responses;

public sealed class AssignRoleToUserResponseDto
{
   public long UserRoleId { get; init; }
    public long UserId { get; init; }
    public long RoleId { get; init; }

    public bool IsAssigned { get; init; }
    public bool WasAlreadyAssigned { get; init; }
}