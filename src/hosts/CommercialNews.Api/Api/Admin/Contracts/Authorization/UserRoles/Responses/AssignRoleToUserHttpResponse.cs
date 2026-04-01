namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Responses;

public sealed class AssignRoleToUserHttpResponse
{
    public long UserRoleId { get; init; }
    public long UserId { get; init; }
    public long RoleId { get; init; }
    public bool IsAssigned { get; init; }
    public bool WasAlreadyAssigned { get; init; }
}