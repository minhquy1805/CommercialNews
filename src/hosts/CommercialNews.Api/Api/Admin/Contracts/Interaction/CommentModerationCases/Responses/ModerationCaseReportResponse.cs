namespace CommercialNews.Api.Api.Admin.Contracts.Interaction.CommentModerationCases.Responses;

public sealed class ModerationCaseReportResponse
{
    public string CommentReportPublicId { get; init; } = string.Empty;

    public long ReporterUserId { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}