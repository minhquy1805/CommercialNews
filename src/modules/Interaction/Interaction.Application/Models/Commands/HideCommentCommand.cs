namespace Interaction.Application.Models.Commands;

public sealed record HideCommentCommand(
    string CommentPublicId,
    long ExpectedVersion,
    string ReasonCode,
    string? Note = null);