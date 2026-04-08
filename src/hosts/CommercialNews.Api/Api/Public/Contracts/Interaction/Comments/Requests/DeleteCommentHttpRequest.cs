namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;

public sealed class DeleteCommentHttpRequest
{
    public long CommentId { get; init; }
}