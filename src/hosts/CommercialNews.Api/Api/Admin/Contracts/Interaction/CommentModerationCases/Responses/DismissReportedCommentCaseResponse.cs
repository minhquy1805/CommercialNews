namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Responses;

public sealed class DismissReportedCommentCaseResponse
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime ResolvedAtUtc { get; init; }

    public long Version { get; init; }
}