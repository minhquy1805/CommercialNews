namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Requests;

public sealed class CreateRoleHttpRequest
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
}