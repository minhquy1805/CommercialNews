using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditIngestion;

public static class AuditIngestionStatuses
{
    public const string Processing = "Processing";
    public const string Succeeded = "Succeeded";
    public const string Duplicate = "Duplicate";
    public const string Ignored = "Ignored";
    public const string Failed = "Failed";
    public const string DeadLettered = "DeadLettered";

    public static readonly FrozenSet<string> All = new[]
    {
        Processing,
        Succeeded,
        Duplicate,
        Ignored,
        Failed,
        DeadLettered
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenSet<string> TerminalStatuses = new[]
    {
        Succeeded,
        Duplicate,
        Ignored,
        DeadLettered
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }

    public static bool IsTerminal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return TerminalStatuses.Contains(value);
    }
}