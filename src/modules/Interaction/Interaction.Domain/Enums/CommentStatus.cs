namespace Interaction.Domain.Enums;
public static class CommentStatus
{
    public const string Visible = "Visible";
    public const string Hidden = "Hidden";
    public const string Deleted = "Deleted";
    public const string Pending = "Pending";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Visible,
        Hidden,
        Deleted,
        Pending
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