namespace Reading.Domain.Constants;

public static class ReadingSortValues
{
    public const string PublishedAtAscending = "publishedAt";
    public const string PublishedAtDescending = "-publishedAt";
    public const string ViewCountAscending = "viewCount";
    public const string ViewCountDescending = "-viewCount";
    public const string LikeCountAscending = "likeCount";
    public const string LikeCountDescending = "-likeCount";

    public const string Default = PublishedAtDescending;

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PublishedAtAscending,
            PublishedAtDescending,
            ViewCountAscending,
            ViewCountDescending,
            LikeCountAscending,
            LikeCountDescending
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

        return All.FirstOrDefault(
                allowed => string.Equals(
                    allowed,
                    trimmed,
                    StringComparison.OrdinalIgnoreCase))
            ?? Default;
    }

    public static string ToSortBy(string? value)
    {
        string normalized = NormalizeOrDefault(value);

        return normalized.TrimStart('-') switch
        {
            "viewCount" => "ViewCount",
            "likeCount" => "LikeCount",
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
