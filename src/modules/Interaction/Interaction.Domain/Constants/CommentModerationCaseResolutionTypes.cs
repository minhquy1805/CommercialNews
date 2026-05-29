namespace Interaction.Domain.Constants;

public static class CommentModerationCaseResolutionTypes
{
    public const string DismissReportedCase = "DismissReportedCase";
    public const string HideReportedComment = "HideReportedComment";
    public const string CloseCaseByAuthorDeletion = "CloseCaseByAuthorDeletion";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        DismissReportedCase,
        HideReportedComment,
        CloseCaseByAuthorDeletion
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