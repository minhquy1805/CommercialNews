namespace Identity.Application.Models.QueryModels;

public sealed class UserAccountListQuery
{
    public DateTime? FromCreatedAt { get; init; }

    public DateTime? ToCreatedAt { get; init; }

    public string? Status { get; init; }

    public bool? IsEmailVerified { get; init; }

    public string? Query { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int Skip => (Page <= 1 ? 0 : Page - 1) * PageSize;

    public int Take => PageSize <= 0 ? 20 : PageSize;
}