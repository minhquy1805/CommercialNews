namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class UserLoginHistoryResponse
{
    public long UserId { get; init; }

    public IReadOnlyList<UserLoginHistoryItemResponse> Items { get; init; } =
        Array.Empty<UserLoginHistoryItemResponse>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}
