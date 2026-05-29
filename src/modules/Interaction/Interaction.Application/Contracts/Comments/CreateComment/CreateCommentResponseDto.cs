namespace Interaction.Application.Contracts.Comments.CreateComment;

public sealed class CreateCommentResponseDto
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public long Version { get; init; }
}