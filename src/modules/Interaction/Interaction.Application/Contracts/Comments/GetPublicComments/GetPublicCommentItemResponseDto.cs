namespace Interaction.Application.Contracts.Comments.GetPublicComments;

public sealed class GetPublicCommentItemResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}