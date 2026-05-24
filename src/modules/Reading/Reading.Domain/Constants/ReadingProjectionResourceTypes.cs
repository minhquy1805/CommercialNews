namespace Reading.Domain.Constants;

public static class ReadingProjectionResourceTypes
{
    public const string Article = "Article";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Article
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
            Article,
            StringComparison.OrdinalIgnoreCase)
            ? Article
            : null;
    }
}