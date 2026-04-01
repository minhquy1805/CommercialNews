namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Responses;

public sealed class DeactivatePermissionHttpResponse
{
    public long PermissionId { get; init; }
    public bool IsDeactivated { get; init; }
    public bool WasAlreadyDeactivated { get; init; }
}