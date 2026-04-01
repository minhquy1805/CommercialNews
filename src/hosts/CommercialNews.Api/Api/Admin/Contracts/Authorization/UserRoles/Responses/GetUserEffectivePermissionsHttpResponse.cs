namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.UserRoles.Responses;

public sealed class GetUserEffectivePermissionsHttpResponse
{
    public long UserId { get; init; }
    public IReadOnlyCollection<EffectivePermissionItemHttpResponse> Permissions { get; init; } = Array.Empty<EffectivePermissionItemHttpResponse>();
}