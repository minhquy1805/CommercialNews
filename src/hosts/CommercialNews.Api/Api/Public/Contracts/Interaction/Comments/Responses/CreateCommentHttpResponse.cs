namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Responses;

public sealed class CreateCommentHttpResponse
{
    public long CommentId { get; init; }

    public DateTime CreatedAt { get; init; }
}