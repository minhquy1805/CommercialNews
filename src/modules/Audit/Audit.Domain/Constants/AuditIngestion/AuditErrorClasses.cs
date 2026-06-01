using System.Collections.Frozen;

namespace Audit.Domain.Constants.AuditIngestion;

public static class AuditErrorClasses
{
    public const string Transient = "Transient";
    public const string Permanent = "Permanent";
    public const string Ambiguous = "Ambiguous";
    public const string Validation = "Validation";
    public const string Policy = "Policy";
    public const string Redaction = "Redaction";
    public const string Unknown = "Unknown";

    public static readonly FrozenSet<string> All = new[]
    {
        Transient,
        Permanent,
        Ambiguous,
        Validation,
        Policy,
        Redaction,
        Unknown
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