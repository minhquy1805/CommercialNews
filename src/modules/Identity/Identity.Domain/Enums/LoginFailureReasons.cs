namespace Identity.Domain.Enums;

public static class LoginFailureReasons
{
    public const string InvalidCredentials = "InvalidCredentials";
    public const string AccountUnverified = "AccountUnverified";
    public const string AccountLocked = "AccountLocked";
    public const string AccountDisabled = "AccountDisabled";
    public const string RefreshReuseDetected = "RefreshReuseDetected";
    public const string RateLimited = "RateLimited";
    public const string Unknown = "Unknown";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        InvalidCredentials,
        AccountUnverified,
        AccountLocked,
        AccountDisabled,
        RefreshReuseDetected,
        RateLimited,
        Unknown
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }
}