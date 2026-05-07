using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.LoginHistory.GetMyLoginHistory;

public sealed class GetMyLoginHistoryRequestDto
{
    public bool? Succeeded { get; init; }

    public DateTime? FromAttemptedAt { get; init; }

    public DateTime? ToAttemptedAt { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}