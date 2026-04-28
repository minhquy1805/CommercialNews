namespace Notifications.Domain.Enums;

public static class EmailAttemptOutcome
{
    public const string Started = "Started";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string TimedOut = "TimedOut";
    public const string Rejected = "Rejected";
    public const string Skipped = "Skipped";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Started,
        Succeeded,
        Failed,
        TimedOut,
        Rejected,
        Skipped
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value.Trim());
    }

    public static bool IsCompleted(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !string.Equals(value.Trim(), Started, StringComparison.OrdinalIgnoreCase)
            && IsValid(value);
    }
}