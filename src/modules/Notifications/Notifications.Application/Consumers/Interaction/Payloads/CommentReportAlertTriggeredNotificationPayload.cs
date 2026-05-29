namespace Notifications.Application.Consumers.Interaction.Payloads;

public sealed record CommentReportAlertTriggeredNotificationPayload(
    string CommentModerationCasePublicId,
    string CommentPublicId,
    string ArticlePublicId,
    string AlertLevel,
    string AlertReason,
    long DistinctReporterCount,
    string HighestSeverity,
    DateTime TriggeredAtUtc);
