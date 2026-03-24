namespace Authorization.Application.Contracts.Requests;

public sealed class AssignRoleToUserRequestDto
{
    public long UserId { get; init; }
    public long RoleId { get; init; }
}