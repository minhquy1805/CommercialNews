namespace Authorization.Domain.Constants;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    public const string Author = "Author";
    public const string User = "User";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Admin,
        Moderator,
        Author,
        User
    };

    public static bool IsBuiltIn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value.Trim());
    }
}