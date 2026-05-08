namespace Identity.Application.Contracts.LoginHistory.GetUserLoginHistory;

public sealed class GetUserLoginHistoryResponseDto
{
    public long UserId { get; init; }

    public IReadOnlyList<UserLoginHistoryItemDto> Items { get; init; } =
        Array.Empty<UserLoginHistoryItemDto>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}