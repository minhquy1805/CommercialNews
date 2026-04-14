namespace Audit.Application.Models;

public static class AuditIngestionOutcome
{
    public const string Inserted = "Inserted";
    public const string Deduped = "Deduped";
    public const string Failed = "Failed";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Inserted,
        Deduped,
        Failed
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