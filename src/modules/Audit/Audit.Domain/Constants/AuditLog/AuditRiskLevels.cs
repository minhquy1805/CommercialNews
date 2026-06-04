using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditLog;

public static class AuditRiskLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly FrozenSet<string> All = new[]
    {
        Low,
        Medium,
        High,
        Critical
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }
}