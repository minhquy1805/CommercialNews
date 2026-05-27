namespace Interaction.Application.Contracts.CommentModerationCases.DismissReportedCommentCase;

public sealed class DismissReportedCommentCaseRequestDto
{
    public string CasePublicId { get; init; } = string.Empty;

    public long ExpectedCaseVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Note { get; init; }
}