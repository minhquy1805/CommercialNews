namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentReportAlertTriggeredPayload(
    string CommentModerationCasePublicId,
    string CommentPublicId,
    string ArticlePublicId,
    string AlertLevel,
    string AlertReason,
    long DistinctReporterCount,
    string HighestSeverity,
    DateTime TriggeredAtUtc);