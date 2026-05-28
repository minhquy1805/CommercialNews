namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Responses;

public sealed class RestoreCommentResponse
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public long Version { get; init; }
}