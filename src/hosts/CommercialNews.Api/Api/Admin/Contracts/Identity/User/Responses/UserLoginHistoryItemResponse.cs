namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class UserLoginHistoryItemResponse
{
    public long LoginId { get; init; }

    public long? UserId { get; init; }

    public bool Succeeded { get; init; }

    public string? FailureReason { get; init; }

    public DateTime AttemptedAt { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }
}
