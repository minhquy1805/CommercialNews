namespace Notifications.Domain.Enums;

public static class EmailAttemptOutcome
{
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Timeout = "Timeout";
    public const string Suppressed = "Suppressed";
    public const string Skipped = "Skipped";
    public const string ProviderRejected = "ProviderRejected";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Sent,
        Failed,
        Timeout,
        Suppressed,
        Skipped,
        ProviderRejected
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