namespace Interaction.Application.Models.QueryModels;

public static class CommentSortFields
{
    public const string CreatedAt = "CreatedAt";
    public const string CommentId = "CommentId";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        CreatedAt,
        CommentId
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