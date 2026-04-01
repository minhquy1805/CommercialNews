namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.RolePermissions.Responses;

public sealed class GrantPermissionToRoleHttpResponse
{
    public long RolePermissionId { get; init; }
    public long RoleId { get; init; }
    public long PermissionId { get; init; }
    public bool IsGranted { get; init; }
    public bool WasAlreadyGranted { get; init; }
}