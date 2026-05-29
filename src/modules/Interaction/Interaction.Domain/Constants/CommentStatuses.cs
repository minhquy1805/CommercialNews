namespace Interaction.Domain.Constants;

public static class CommentStatuses
{
    public const string Visible = "Visible";
    public const string Hidden = "Hidden";
    public const string Deleted = "Deleted";

    // Reserved for future selective moderation.
    public const string Pending = "Pending";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Visible,
        Hidden,
        Deleted,
        Pending,
        Rejected
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