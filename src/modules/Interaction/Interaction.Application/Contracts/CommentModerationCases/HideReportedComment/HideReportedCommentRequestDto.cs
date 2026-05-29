namespace Interaction.Application.Contracts.CommentModerationCases.HideReportedComment;

public sealed class HideReportedCommentRequestDto
{
    public string CasePublicId { get; init; } = string.Empty;

    public long ExpectedCaseVersion { get; init; }

    public long ExpectedCommentVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Note { get; init; }
}