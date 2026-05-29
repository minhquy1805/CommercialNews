namespace Interaction.Application.Contracts.Comments.GetAdminComments;

public sealed class GetAdminCommentItemResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Reserved for future reply-comment capability.
    /// This value is null for all Interaction V1 comments.
    /// </summary>
    public string? ParentCommentPublicId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? DeletedAtUtc { get; init; }

    public long Version { get; init; }
}