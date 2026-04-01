namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Responses;

public sealed class ActivatePermissionHttpResponse
{
    public long PermissionId { get; init; }
    public bool IsActivated { get; init; }
    public bool WasAlreadyActivated { get; init; }
}