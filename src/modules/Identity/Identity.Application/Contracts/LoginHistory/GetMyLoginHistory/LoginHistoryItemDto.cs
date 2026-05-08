namespace Identity.Application.Contracts.LoginHistory.GetMyLoginHistory;

public sealed class LoginHistoryItemDto
{
    public long LoginId { get; init; }

    public bool Succeeded { get; init; }

    public string? FailureReason { get; init; }

    public DateTime AttemptedAt { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }
}