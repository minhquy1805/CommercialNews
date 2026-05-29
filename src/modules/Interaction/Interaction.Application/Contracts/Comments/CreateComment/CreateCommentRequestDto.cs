namespace Interaction.Application.Contracts.Comments.CreateComment;

public sealed class CreateCommentRequestDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}