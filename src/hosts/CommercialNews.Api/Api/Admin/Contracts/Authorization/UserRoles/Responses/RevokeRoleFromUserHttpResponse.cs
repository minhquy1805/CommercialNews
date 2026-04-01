namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Responses;

public sealed class RevokeRoleFromUserHttpResponse
{
    public long UserId { get; init; }
    public long RoleId { get; init; }
    public bool IsRevoked { get; init; }
    public bool WasAlreadyRevoked { get; init; }
}