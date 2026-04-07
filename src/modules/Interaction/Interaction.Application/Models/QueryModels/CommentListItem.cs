namespace Interaction.Application.Models.QueryModels;

public sealed class CommentListItem
{
    public long CommentId { get; init; }

    public long ArticleId { get; init; }

    public long UserId { get; init; }

    public long? ParentCommentId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public int EditCount { get; init; }
}