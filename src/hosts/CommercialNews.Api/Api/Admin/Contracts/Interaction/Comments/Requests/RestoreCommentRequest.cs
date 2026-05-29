namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Requests;

public sealed class RestoreCommentRequest
{
    public long ExpectedVersion { get; init; }

    public string? Note { get; init; }
}