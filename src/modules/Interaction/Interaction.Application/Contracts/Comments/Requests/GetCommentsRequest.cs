namespace Interaction.Application.Contracts.Comments.Requests;

public sealed class GetCommentsRequest
{
    public long ArticleId { get; init; }

    public long? ParentCommentId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = "CreatedAt";

    public string SortDirection { get; init; } = "DESC";
}