namespace Reading.Domain.Constants;

public static class ReadingProjectionScopes
{
    public const string Public = "public";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Public
        };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && All.Contains(value.Trim());
    }

    public static string? NormalizeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Equals(
            value.Trim(),
            Public,
            StringComparison.OrdinalIgnoreCase)
            ? Public
            : null;
    }
}