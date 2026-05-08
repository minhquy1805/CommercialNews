namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class UserListResponse
{
    public IReadOnlyList<UserListItemResponse> Items { get; init; } =
        Array.Empty<UserListItemResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}
