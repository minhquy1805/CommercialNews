namespace Interaction.Application.Models.Commands;

public sealed record DeleteOwnCommentCommand(
    string CommentPublicId,
    long? ExpectedVersion = null);