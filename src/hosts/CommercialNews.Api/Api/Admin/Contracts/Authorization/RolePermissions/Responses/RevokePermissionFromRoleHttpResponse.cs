namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;

public sealed class RevokePermissionFromRoleHttpResponse
{
    public long RoleId { get; init; }
    public long PermissionId { get; init; }
    public bool IsRevoked { get; init; }
    public bool WasAlreadyRevoked { get; init; }
}