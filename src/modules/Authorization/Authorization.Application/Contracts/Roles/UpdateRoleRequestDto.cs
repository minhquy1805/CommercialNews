namespace Authorization.Application.Contracts.Roles;

public sealed class UpdateRoleRequestDto
{
    public long RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}