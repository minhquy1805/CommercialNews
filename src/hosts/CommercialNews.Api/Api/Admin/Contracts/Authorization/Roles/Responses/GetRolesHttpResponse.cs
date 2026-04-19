namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Responses;

public sealed class GetRolesHttpResponse
{
    public IReadOnlyList<RoleListItemHttpResponse> Items { get; init; }
        = Array.Empty<RoleListItemHttpResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}