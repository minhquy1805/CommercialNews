namespace Interaction.Application.Contracts.CommentReports.CreateCommentReport;

public sealed class CreateCommentReportResponseDto
{
    public string CommentReportPublicId { get; init; } = string.Empty;

    public string CommentPublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}