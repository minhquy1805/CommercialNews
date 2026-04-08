namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;

public sealed class CreateCommentHttpRequest
{
    public long? ParentCommentId { get; init; }

    public string Content { get; init; } = string.Empty;
}