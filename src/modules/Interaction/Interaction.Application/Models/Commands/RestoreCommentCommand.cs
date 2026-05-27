namespace Interaction.Application.Models.Commands;

public sealed record RestoreCommentCommand(
    string CommentPublicId,
    long ExpectedVersion,
    string? Note = null);