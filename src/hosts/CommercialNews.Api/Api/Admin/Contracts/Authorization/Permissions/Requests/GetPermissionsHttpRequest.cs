namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Permissions.Requests;

public sealed class GetPermissionsHttpRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Query { get; init; }
    public string? Module { get; init; }
    public string? Action { get; init; }
    public bool? IsActive { get; init; }
}