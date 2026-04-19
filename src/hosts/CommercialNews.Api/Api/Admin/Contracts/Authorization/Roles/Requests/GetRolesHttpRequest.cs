namespace CommercialNews.Api.Api.Admin.Contracts.Authorization.Roles.Requests;

public sealed class GetRolesHttpRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Query { get; init; }
    public bool? IsActive { get; init; }
}