namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Requests;

public sealed class GrantPermissionToRoleHttpRequest
{
    public long PermissionId { get; init; }
}