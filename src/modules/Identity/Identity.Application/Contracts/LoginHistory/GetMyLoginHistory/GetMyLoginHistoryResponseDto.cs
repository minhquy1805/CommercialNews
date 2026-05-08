namespace Identity.Application.Contracts.LoginHistory.GetMyLoginHistory;

public sealed class GetMyLoginHistoryResponseDto
{
    public IReadOnlyList<LoginHistoryItemDto> Items { get; init; } =
        Array.Empty<LoginHistoryItemDto>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}