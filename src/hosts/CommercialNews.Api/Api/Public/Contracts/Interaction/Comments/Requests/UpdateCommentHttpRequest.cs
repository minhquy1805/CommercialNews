namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;

public sealed class UpdateCommentHttpRequest
{
    public string Content { get; init; } = string.Empty;
}