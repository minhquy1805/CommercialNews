namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.Comments.Requests;

public sealed class HideCommentRequest
{
    public long ExpectedVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Note { get; init; }
}