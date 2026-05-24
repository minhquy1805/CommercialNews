namespace Reading.Domain.Constants;

public static class ProjectionApplyDecisions
{
    public const string Applied = "Applied";
    public const string Ignored = "Ignored";
    public const string IgnoredStaleVersion = "IgnoredStaleVersion";
    public const string IgnoredStaleOrMissing = "IgnoredStaleOrMissing";
    public const string IgnoredMissingArticle = "IgnoredMissingArticle";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Applied,
            Ignored,
            IgnoredStaleVersion,
            IgnoredStaleOrMissing,
            IgnoredMissingArticle
        };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && All.Contains(value.Trim());
    }

    public static bool IsApplied(string? value)
    {
        return string.Equals(
            value?.Trim(),
            Applied,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIgnored(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();

        return string.Equals(trimmed, Ignored, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, IgnoredStaleVersion, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, IgnoredStaleOrMissing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, IgnoredMissingArticle, StringComparison.OrdinalIgnoreCase);
    }
}