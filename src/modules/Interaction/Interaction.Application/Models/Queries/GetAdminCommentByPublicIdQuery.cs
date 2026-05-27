namespace Interaction.Application.Models.Queries;

public sealed record GetAdminCommentByPublicIdQuery(
    string CommentPublicId);