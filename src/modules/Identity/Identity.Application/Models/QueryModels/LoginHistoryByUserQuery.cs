namespace Identity.Application.Models.QueryModels;

public sealed class LoginHistoryByUserQuery
{
    public long UserId { get; init; }

    public bool? Succeeded { get; init; }

    public DateTime? FromAttemptedAt { get; init; }

    public DateTime? ToAttemptedAt { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int Skip => (Page <= 1 ? 0 : Page - 1) * PageSize;

    public int Take => PageSize <= 0 ? 20 : PageSize;
}