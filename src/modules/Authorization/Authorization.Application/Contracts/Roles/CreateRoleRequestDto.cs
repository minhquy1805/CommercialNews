namespace Authorization.Application.Contracts.Roles;

public sealed class CreateRoleRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
}