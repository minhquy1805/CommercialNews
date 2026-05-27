namespace Interaction.Application.Contracts.Comments.GetCommentModerationHistory;

public sealed class GetCommentModerationHistoryRequestDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}