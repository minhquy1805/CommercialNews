using Content.Domain.Exceptions;

namespace Content.Domain.Constants;

public static class ArticleLifecycleActionTypes
{
    public const string Publish = "Publish";
    public const string Unpublish = "Unpublish";
    public const string Archive = "Archive";
    public const string SoftDelete = "SoftDelete";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Publish,
        Unpublish,
        Archive,
        SoftDelete
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
                "CONTENT.ARTICLE_LIFECYCLE_ACTION_TYPE_INVALID",
                "Lifecycle action type is invalid.");
        }

        string trimmed = value.Trim();

        if (string.Equals(trimmed, Publish, StringComparison.OrdinalIgnoreCase))
        {
            return Publish;
        }

        if (string.Equals(trimmed, Unpublish, StringComparison.OrdinalIgnoreCase))
        {
            return Unpublish;
        }

        if (string.Equals(trimmed, Archive, StringComparison.OrdinalIgnoreCase))
        {
            return Archive;
        }

        if (string.Equals(trimmed, SoftDelete, StringComparison.OrdinalIgnoreCase))
        {
            return SoftDelete;
        }

        throw new ContentDomainException(
            "CONTENT.ARTICLE_LIFECYCLE_ACTION_TYPE_INVALID",
            "Lifecycle action type is invalid.");
    }
}
