namespace Authorization.Application.Contracts.Roles;

public sealed class GetRolesRequestDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Query { get; init; }
    public bool? IsActive { get; init; }
}