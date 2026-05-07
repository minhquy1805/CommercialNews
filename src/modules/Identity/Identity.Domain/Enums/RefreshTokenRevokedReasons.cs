namespace Identity.Domain.Enums;

public static class RefreshTokenRevokedReasons
{
    public const string Logout = "Logout";
    public const string LogoutAll = "LogoutAll";
    public const string Rotated = "Rotated";
    public const string PasswordReset = "PasswordReset";
    public const string PasswordChanged = "PasswordChanged";
    public const string AdminSecurityAction = "AdminSecurityAction";
    public const string ReuseDetected = "ReuseDetected";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Logout,
        LogoutAll,
        Rotated,
        PasswordReset,
        PasswordChanged,
        AdminSecurityAction,
        ReuseDetected
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