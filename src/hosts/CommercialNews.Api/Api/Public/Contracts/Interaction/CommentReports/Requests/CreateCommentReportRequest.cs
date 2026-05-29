namespace CommercialNews.Api.Api.Public.Contracts.Interaction.CommentReports.Requests;

public sealed class CreateCommentReportRequest
{
    public string ReasonCode { get; init; } = string.Empty;

    public string? Description { get; init; }
}