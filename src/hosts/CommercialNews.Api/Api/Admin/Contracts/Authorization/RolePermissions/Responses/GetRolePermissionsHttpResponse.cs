namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;

public sealed class GetRolePermissionsHttpResponse
{
    public long RoleId { get; init; }
    public IReadOnlyCollection<RolePermissionItemHttpResponse> Permissions { get; init; } = Array.Empty<RolePermissionItemHttpResponse>();
}