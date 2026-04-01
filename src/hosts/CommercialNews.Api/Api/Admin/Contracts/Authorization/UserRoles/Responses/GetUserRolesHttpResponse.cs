namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Responses;

public sealed class GetUserRolesHttpResponse
{
    public long UserId { get; init; }
    public IReadOnlyCollection<UserRoleItemHttpResponse> Roles { get; init; } = Array.Empty<UserRoleItemHttpResponse>();
}