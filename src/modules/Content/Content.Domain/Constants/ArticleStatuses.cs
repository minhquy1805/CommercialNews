using Content.Domain.Exceptions;

namespace Content.Domain.Constants;

public static class ArticleStatuses
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

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ContentDomainException(
                "CONTENT.ARTICLE_INVALID_STATUS",
                "Article status is invalid.");
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

        throw new ContentDomainException(
            "CONTENT.ARTICLE_INVALID_STATUS",
            "Article status is invalid.");
    }
}
