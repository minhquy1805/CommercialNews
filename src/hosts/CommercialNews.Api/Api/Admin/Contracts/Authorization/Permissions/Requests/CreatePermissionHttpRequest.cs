namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Requests;

public sealed class CreatePermissionHttpRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public bool IsSystem { get; init; }
}