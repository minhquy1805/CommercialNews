namespace Reading.Domain.Constants;

public static class ReadingSortValues
{
    public const string PublishedAtAscending = "publishedAt";
    public const string PublishedAtDescending = "-publishedAt";
    public const string PopularityAscending = "popularity";
    public const string PopularityDescending = "-popularity";

    public const string Default = PublishedAtDescending;

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PublishedAtAscending,
        PublishedAtDescending,
        PopularityAscending,
        PopularityDescending
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value.Trim());
    }

    public static string NormalizeOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Default;
        }

        string trimmed = value.Trim();

        if (string.Equals(trimmed, PublishedAtAscending, StringComparison.OrdinalIgnoreCase))
        {
            return PublishedAtAscending;
        }

        if (string.Equals(trimmed, PublishedAtDescending, StringComparison.OrdinalIgnoreCase))
        {
            return PublishedAtDescending;
        }

        if (string.Equals(trimmed, PopularityAscending, StringComparison.OrdinalIgnoreCase))
        {
            return PopularityAscending;
        }

        if (string.Equals(trimmed, PopularityDescending, StringComparison.OrdinalIgnoreCase))
        {
            return PopularityDescending;
        }

        return Default;
    }

    public static string ToSortBy(string? value)
    {
        string normalized = NormalizeOrDefault(value);

        return normalized switch
        {
            PublishedAtAscending or PublishedAtDescending => "PublishedAt",
            PopularityAscending or PopularityDescending => "Popularity",
            _ => "PublishedAt"
        };
    }

    public static string ToSortDirection(string? value)
    {
        string normalized = NormalizeOrDefault(value);

        return normalized.StartsWith("-", StringComparison.Ordinal)
            ? "DESC"
            : "ASC";
    }
}