namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Requests;

public sealed class HideReportedCommentRequest
{
    public long ExpectedCaseVersion { get; init; }

    public long ExpectedCommentVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Note { get; init; }
}