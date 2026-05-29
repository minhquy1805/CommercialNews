namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Responses;

public sealed class ModerationCaseCommentResponse
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long Version { get; init; }
}