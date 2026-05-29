namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Responses;

public sealed class PublicCommentItemResponse
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}