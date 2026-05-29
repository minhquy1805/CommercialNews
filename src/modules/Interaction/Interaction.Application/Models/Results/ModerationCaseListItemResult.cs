namespace Interaction.Application.Models.Results;

public sealed record ModerationCaseListItemResult(
    string CommentModerationCasePublicId,
    string CommentPublicId,
    string ArticlePublicId,
    string Status,
    string Priority,
    string HighestSeverity,
    int PendingReportCount,
    int DistinctReporterCount,
    bool AlertTriggered,
    DateTime? AlertTriggeredAtUtc,
    string? AlertLevel,
    DateTime OpenedAtUtc,
    long Version);