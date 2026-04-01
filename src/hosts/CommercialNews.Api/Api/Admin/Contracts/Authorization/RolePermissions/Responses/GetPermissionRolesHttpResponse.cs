namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;

public sealed class GetPermissionRolesHttpResponse
{
    public long PermissionId { get; init; }
    public IReadOnlyCollection<PermissionRoleItemHttpResponse> Roles { get; init; } = Array.Empty<PermissionRoleItemHttpResponse>();
}