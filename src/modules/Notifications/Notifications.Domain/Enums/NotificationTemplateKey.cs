namespace Notifications.Domain.Enums;

public static class NotificationTemplateKey
{
    public const string VerifyEmail = "VerifyEmail";
    public const string ResetPassword = "ResetPassword";
    public const string NewArticle = "NewArticle";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        VerifyEmail,
        ResetPassword,
        NewArticle
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