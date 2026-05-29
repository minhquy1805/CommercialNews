namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Responses;

public sealed class ModerationCaseItemResponse
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public string HighestSeverity { get; init; } = string.Empty;

    public int PendingReportCount { get; init; }

    public int DistinctReporterCount { get; init; }

    public bool AlertTriggered { get; init; }

    public DateTime? AlertTriggeredAtUtc { get; init; }

    public string? AlertLevel { get; init; }

    public DateTime OpenedAtUtc { get; init; }

    public long Version { get; init; }
}