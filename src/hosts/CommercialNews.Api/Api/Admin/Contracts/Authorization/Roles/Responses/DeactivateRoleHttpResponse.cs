namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Responses;

public sealed class DeactivateRoleHttpResponse
{
    public long RoleId { get; init; }
    public bool IsDeactivated { get; init; }
    public bool WasAlreadyDeactivated { get; init; }
}