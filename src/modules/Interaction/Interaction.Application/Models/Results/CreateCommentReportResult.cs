namespace Interaction.Application.Models.Results;

public sealed record CreateCommentReportResult(
    string CommentReportPublicId,
    string CommentPublicId,
    string Status,
    DateTime CreatedAtUtc);