namespace Interaction.Application.Models.Results;

public sealed record HideReportedCommentResult(
    string CommentModerationCasePublicId,
    string CaseStatus,
    long CaseVersion,
    DateTime? ResolvedAtUtc,
    string CommentPublicId,
    string ArticlePublicId,
    string CommentStatus,
    long CommentVersion,
    DateTime? HiddenAtUtc,
    long ResolvedReportCount);