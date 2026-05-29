namespace Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId;

public sealed class GetAdminCommentByPublicIdResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Reserved for future reply-comment capability.
    /// Interaction V1 currently supports top-level comments only.
    /// </summary>
    public long? ParentCommentId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? DeletedAtUtc { get; init; }

    public long Version { get; init; }
}