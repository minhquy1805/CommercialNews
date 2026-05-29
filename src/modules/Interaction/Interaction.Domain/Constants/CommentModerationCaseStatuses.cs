namespace Interaction.Domain.Constants;

public static class CommentModerationCaseStatuses
{
    public const string Open = "Open";
    public const string Dismissed = "Dismissed";
    public const string Actioned = "Actioned";
    public const string ClosedByAuthorDeletion = "ClosedByAuthorDeletion";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Open,
        Dismissed,
        Actioned,
        ClosedByAuthorDeletion
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