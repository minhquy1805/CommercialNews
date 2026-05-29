namespace Notifications.Application.Configuration;

public sealed class InteractionAlertEmailOptions
{
    public const string SectionName = "Notifications:InteractionAlerts";

    public string CommentReportAlertRecipientEmail { get; init; } = string.Empty;

    public byte CommentReportAlertPriority { get; init; } = 1;

    public string ModerationCaseUrlTemplate { get; init; } =
        "http://localhost:3000/admin/interaction/comment-moderation-cases/{casePublicId}";
}
