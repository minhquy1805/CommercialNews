namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Comments.Requests;

public sealed class DeleteOwnCommentRequest
{
    public long? ExpectedVersion { get; init; }
}