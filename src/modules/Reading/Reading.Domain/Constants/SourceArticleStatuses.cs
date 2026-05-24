namespace Reading.Domain.Constants;

public static class SourceArticleStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Draft,
        Published,
        Archived
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value.Trim());
    }

    public static bool IsPublished(string? value)
    {
        return string.Equals(
            value?.Trim(),
            Published,
            StringComparison.OrdinalIgnoreCase);
    }

    public static string? NormalizeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (string.Equals(trimmed, Draft, StringComparison.OrdinalIgnoreCase))
        {
            return Draft;
        }

        if (string.Equals(trimmed, Published, StringComparison.OrdinalIgnoreCase))
        {
            return Published;
        }

        if (string.Equals(trimmed, Archived, StringComparison.OrdinalIgnoreCase))
        {
            return Archived;
        }

        return null;
    }
}