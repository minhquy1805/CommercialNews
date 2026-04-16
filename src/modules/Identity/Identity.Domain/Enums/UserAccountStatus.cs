namespace Identity.Domain.Enums;

public static class UserAccountStatuses
{
    public const string Unverified = "Unverified";
    public const string Active = "Active";
    public const string Locked = "Locked";
    public const string Disabled = "Disabled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Unverified,
        Active,
        Locked,
        Disabled
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