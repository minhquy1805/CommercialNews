namespace Reading.Domain.Enums;

public static class ReadingSortValues
{
    public const string PublishedAtAscending = "publishedAt";
    public const string PublishedAtDescending = "-publishedAt";
    public const string PopularityAscending = "popularity";
    public const string PopularityDescending = "-popularity";

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

        return All.Contains(value);
    }
}