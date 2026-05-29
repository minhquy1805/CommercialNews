namespace Interaction.Application.Models.Queries;

public sealed record GetCommentModerationHistoryQuery(
    string CommentPublicId,
    int Page,
    int PageSize);