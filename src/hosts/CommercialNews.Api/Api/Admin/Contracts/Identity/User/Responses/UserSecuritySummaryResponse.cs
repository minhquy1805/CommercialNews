namespace CommercialNews.Api.Api.Admin.Contracts.Identity.User.Responses;

public sealed class UserSecuritySummaryResponse
{
    public long UserId { get; init; }

    public string PublicId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public bool IsEmailVerified { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? LockedUntil { get; init; }

    public DateTime? LastLoginAt { get; init; }

    public int TotalSessionCount { get; init; }

    public int ActiveSessionCount { get; init; }

    public int RevokedSessionCount { get; init; }

    public int ExpiredSessionCount { get; init; }

    public int LoginSuccessCount { get; init; }

    public int LoginFailureCount { get; init; }

    public int FailedLoginCountLast7Days { get; init; }

    public DateTime? RecentFailedLoginAt { get; init; }

    public DateTime? LastPasswordResetRequestedAt { get; init; }

    public int PasswordResetTokenCount { get; init; }

    public int ActivePasswordResetTokenCount { get; init; }
}
