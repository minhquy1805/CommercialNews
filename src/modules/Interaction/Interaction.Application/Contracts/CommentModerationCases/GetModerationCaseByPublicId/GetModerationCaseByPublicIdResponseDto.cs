namespace Interaction.Application.Contracts.CommentModerationCases.GetModerationCaseByPublicId;

public sealed class GetModerationCaseByPublicIdResponseDto
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public string HighestSeverity { get; init; } = string.Empty;

    public DateTime? AlertTriggeredAtUtc { get; init; }

    public string? AlertLevel { get; init; }

    public DateTime OpenedAtUtc { get; init; }

    public DateTime? ResolvedAtUtc { get; init; }

    public string? ResolutionType { get; init; }

    public string? ResolutionReasonCode { get; init; }

    public string? ResolutionNote { get; init; }

    public long Version { get; init; }

    public ModerationCaseCommentResponseDto Comment { get; init; } = new();

    public IReadOnlyList<ModerationCaseReportResponseDto> Reports { get; init; } =
        Array.Empty<ModerationCaseReportResponseDto>();
}