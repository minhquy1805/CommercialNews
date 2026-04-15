using CommercialNews.BuildingBlocks.SharedKernel.Paging;

namespace Interaction.Application.Contracts.Comments.Responses;

public sealed class GetCommentsResponse
{
    public IReadOnlyList<CommentItemResponse> Items { get; init; } = Array.Empty<CommentItemResponse>();

    public PageInfo PageInfo { get; init; } = new();
}

public sealed class CommentItemResponse
{
    public long CommentId { get; init; }

    public long ArticleId { get; init; }

    public long UserId { get; init; }

    public long? ParentCommentId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public int EditCount { get; init; }
}