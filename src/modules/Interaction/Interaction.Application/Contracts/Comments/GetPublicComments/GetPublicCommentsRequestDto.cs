namespace Interaction.Application.Contracts.Comments.GetPublicComments;

public sealed class GetPublicCommentsRequestDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortDirection { get; init; } = "DESC";
}