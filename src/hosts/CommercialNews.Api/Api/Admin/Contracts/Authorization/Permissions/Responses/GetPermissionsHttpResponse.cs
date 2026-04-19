namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Responses;

public sealed class GetPermissionsHttpResponse
{
    public IReadOnlyList<PermissionListItemHttpResponse> Items { get; init; }
        = Array.Empty<PermissionListItemHttpResponse>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
}