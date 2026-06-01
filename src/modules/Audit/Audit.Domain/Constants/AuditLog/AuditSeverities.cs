using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditLog;

public static class AuditSeverities
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";

    public static readonly FrozenSet<string> All = new[]
    {
        Info,
        Warning,
        Error,
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