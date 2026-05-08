namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class UserSessionListResponse
{
    public long UserId { get; init; }

    public IReadOnlyList<UserSessionItemResponse> Items { get; init; } =
        Array.Empty<UserSessionItemResponse>();
}
