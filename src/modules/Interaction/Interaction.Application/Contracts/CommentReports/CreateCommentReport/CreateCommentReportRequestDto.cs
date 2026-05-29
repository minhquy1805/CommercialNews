namespace Interaction.Application.Contracts.CommentReports.CreateCommentReport;

public sealed class CreateCommentReportRequestDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    public string? Description { get; init; }
}