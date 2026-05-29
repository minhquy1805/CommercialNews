namespace Reading.Domain.Constants;

public static class ReadingSortValues
{
    public const string PublishedAtAscending = "publishedAt";
    public const string PublishedAtDescending = "-publishedAt";

    public const string Default = PublishedAtDescending;

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PublishedAtAscending,
            PublishedAtDescending
        };

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && All.Contains(value.Trim());
    }

    public static string NormalizeOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        string trimmed = value.Trim();

        if (string.Equals(
            trimmed,
            PublishedAtAscending,
            StringComparison.OrdinalIgnoreCase))
        {
            return PublishedAtAscending;
        }

        if (string.Equals(
            trimmed,
            PublishedAtDescending,
            StringComparison.OrdinalIgnoreCase))
        {
            return PublishedAtDescending;
        }

        return Default;
    }

    public static string ToSortBy(string? value)
    {
        return "PublishedAt";
    }

    public static string ToSortDirection(string? value)
    {
        string normalized = NormalizeOrDefault(value);

        return normalized.StartsWith("-", StringComparison.Ordinal)
            ? "DESC"
            : "ASC";
    }
}