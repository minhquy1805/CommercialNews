namespace Interaction.Application.Contracts.Comments.DeleteOwnComment;

public sealed class DeleteOwnCommentResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime DeletedAtUtc { get; init; }

    public long Version { get; init; }
}