namespace Interaction.Application.Models.Commands;

public sealed record CreateCommentReportCommand(
    string CommentPublicId,
    string ReasonCode,
    string? Description = null);