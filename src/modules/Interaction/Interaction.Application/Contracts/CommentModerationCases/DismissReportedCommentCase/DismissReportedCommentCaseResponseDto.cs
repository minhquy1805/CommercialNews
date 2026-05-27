namespace Interaction.Application.Contracts.CommentModerationCases.DismissReportedCommentCase;

public sealed class DismissReportedCommentCaseResponseDto
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime ResolvedAtUtc { get; init; }

    public long Version { get; init; }
}