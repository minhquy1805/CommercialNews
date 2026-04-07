namespace Interaction.Application.Models.QueryModels;

public sealed class CommentListQuery
{
    public long ArticleId { get; init; }

    public long? ParentCommentId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string SortBy { get; init; } = CommentSortFields.CreatedAt;

    public string SortDirection { get; init; } = "DESC";
}