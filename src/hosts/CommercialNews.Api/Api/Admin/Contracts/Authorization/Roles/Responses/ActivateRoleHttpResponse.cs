namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Responses;

public sealed class ActivateRoleHttpResponse
{
    public long RoleId { get; init; }
    public bool IsActivated { get; init; }
    public bool WasAlreadyActivated { get; init; }
}