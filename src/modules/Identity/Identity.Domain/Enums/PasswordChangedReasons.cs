namespace Identity.Domain.Enums;

public static class PasswordChangedReasons
{
    public const string ChangedByUser = "ChangedByUser";
    public const string ResetByUser = "ResetByUser";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ChangedByUser,
            ResetByUser
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