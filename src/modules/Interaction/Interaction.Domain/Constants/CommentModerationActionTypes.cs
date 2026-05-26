namespace Interaction.Domain.Constants;

public static class CommentModerationActionTypes
{
    public const string Hide = "Hide";
    public const string Restore = "Restore";
    public const string DismissReportedCase = "DismissReportedCase";
    public const string HideReportedComment = "HideReportedComment";
    public const string CloseCaseByAuthorDeletion = "CloseCaseByAuthorDeletion";

    // Reserved for future selective moderation.
    public const string Approve = "Approve";
    public const string Reject = "Reject";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Hide,
        Restore,
        DismissReportedCase,
        HideReportedComment,
        CloseCaseByAuthorDeletion,
        Approve,
        Reject
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