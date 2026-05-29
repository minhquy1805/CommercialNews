namespace Interaction.Application.Models.Results;

public sealed record DismissReportedCommentCaseResult(
    string CommentModerationCasePublicId,
    string CaseStatus,
    long CaseVersion,
    DateTime? ResolvedAtUtc,
    long? ResolvedByUserId,
    string? ResolutionType,
    string? ResolutionReasonCode);