namespace Notifications.Domain.Enums;

public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Published = "Published";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Processing,
        Published,
        Failed,
        DeadLetter
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