namespace Interaction.Application.Contracts.CommentModerationCases.HideReportedComment;

public sealed class HideReportedCommentResponseDto
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string CaseStatus { get; init; } = string.Empty;

    public long CaseVersion { get; init; }

    public string CommentPublicId { get; init; } = string.Empty;

    public string CommentStatus { get; init; } = string.Empty;

    public long CommentVersion { get; init; }

    public DateTime ResolvedAtUtc { get; init; }

    public DateTime HiddenAtUtc { get; init; }
}