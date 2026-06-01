using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditLog;

public static class AuditOutcomes
{
    public const string Success = "Success";
    public const string Failure = "Failure";
    public const string Denied = "Denied";
    public const string Ignored = "Ignored";

    public static readonly FrozenSet<string> All = new[]
    {
        Success,
        Failure,
        Denied,
        Ignored
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