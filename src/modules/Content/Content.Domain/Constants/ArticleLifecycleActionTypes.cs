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

        return All.Contains(value);
    }
}