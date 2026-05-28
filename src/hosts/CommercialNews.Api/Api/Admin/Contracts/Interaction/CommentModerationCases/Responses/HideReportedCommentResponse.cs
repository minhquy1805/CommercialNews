namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Responses;

public sealed class HideReportedCommentResponse
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string CaseStatus { get; init; } = string.Empty;

    public long CaseVersion { get; init; }

    public string CommentPublicId { get; init; } = string.Empty;

    public string CommentStatus { get; init; } = string.Empty;

    public long CommentVersion { get; init; }

    public DateTime ResolvedAtUtc { get; init; }

    public DateTime HiddenAtUtc { get; init; }
}