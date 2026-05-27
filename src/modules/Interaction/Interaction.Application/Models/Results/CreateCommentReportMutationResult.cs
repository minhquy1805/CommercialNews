namespace Interaction.Application.Models.Results;

public sealed record CreateCommentReportMutationResult(
    string CommentReportPublicId,
    string CommentPublicId,
    string ArticlePublicId,
    long ReporterUserId,
    string ReasonCode,
    string? Description,
    string ReportStatus,
    DateTime CreatedAtUtc,
    string CommentModerationCasePublicId,
    string CaseStatus,
    string Priority,
    string HighestSeverity,
    long DistinctReporterCount,
    DateTime? AlertTriggeredAtUtc,
    string? AlertLevel,
    long CaseVersion,
    bool AlertTriggered,
    bool CreatedNewCase);