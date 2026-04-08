namespace Notifications.Domain.Enums;

public static class EmailDeliveryStatus
{
    public const string Queued = "Queued";
    public const string Sending = "Sending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Dead = "Dead";
    public const string Suppressed = "Suppressed";
    public const string Ambiguous = "Ambiguous";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Queued,
        Sending,
        Sent,
        Failed,
        Dead,
        Suppressed,
        Ambiguous
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