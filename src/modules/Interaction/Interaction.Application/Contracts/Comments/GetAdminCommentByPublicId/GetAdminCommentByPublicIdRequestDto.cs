namespace Interaction.Application.Contracts.Comments.GetAdminCommentByPublicId;

public sealed class GetAdminCommentByPublicIdRequestDto
{
    public string CommentPublicId { get; init; } = string.Empty;
}