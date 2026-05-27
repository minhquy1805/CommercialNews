namespace Interaction.Application.Models.Results;

public sealed record ModerationCaseDetailResult(
    string CommentModerationCasePublicId,
    string Status,
    string Priority,
    string HighestSeverity,
    DateTime? AlertTriggeredAtUtc,
    string? AlertLevel,
    string? AlertMessageId,
    DateTime OpenedAtUtc,
    DateTime? ResolvedAtUtc,
    long? ResolvedByUserId,
    string? ResolutionType,
    string? ResolutionReasonCode,
    string? ResolutionNote,
    long Version,
    ModerationCaseCommentDetailResult Comment,
    IReadOnlyList<ModerationCaseReportDetailResult> Reports);