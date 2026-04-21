namespace CommercialNews.BuildingBlocks.Messaging.Outbox;

public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Publishing = "Publishing";
    public const string Published = "Published";
    public const string Failed = "Failed";
    public const string Dead = "Dead";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Publishing,
        Published,
        Failed,
        Dead
    };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && All.Contains(value);
    }
}