namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Requests;

public sealed class UpdatePermissionHttpRequest
{
    public string Key { get; init; } = string.Empty;
    public string? Module { get; init; }
    public string? Action { get; init; }
    public string? Description { get; init; }
}