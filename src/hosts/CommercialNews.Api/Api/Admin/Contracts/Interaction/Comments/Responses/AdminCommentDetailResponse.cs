namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Responses;

public sealed class AdminCommentDetailResponse
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? ParentCommentPublicId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? DeletedAtUtc { get; init; }

    public long Version { get; init; }
}