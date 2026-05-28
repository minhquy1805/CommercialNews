namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Requests;

public sealed class DismissReportedCommentCaseRequest
{
    public long ExpectedCaseVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Note { get; init; }
}