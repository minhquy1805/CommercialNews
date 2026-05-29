namespace Interaction.Application.Contracts.Comments.GetAdminComments;

public sealed class GetAdminCommentsRequestDto
{
    public string? Status { get; init; }

    public string? ArticlePublicId { get; init; }

    public long? AuthorUserId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}