namespace Notifications.Domain.Enums;

public static class EmailDeliveryStatus
{
    public const string Queued = "Queued";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Dead = "Dead";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Queued,
        Sending,
        Sent,
        Failed,
        Dead
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value.Trim());
    }

    public static bool IsTerminal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        return string.Equals(normalized, Sent, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Dead, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRetryable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value.Trim(), Failed, StringComparison.OrdinalIgnoreCase);
    }
}