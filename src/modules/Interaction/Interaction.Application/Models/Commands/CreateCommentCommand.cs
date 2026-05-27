namespace Interaction.Application.Models.Commands;

public sealed record CreateCommentCommand(
    string ArticlePublicId,
    string Content);