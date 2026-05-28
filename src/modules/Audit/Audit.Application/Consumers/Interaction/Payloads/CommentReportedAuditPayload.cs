namespace Audit.Application.Consumers.Interaction.Payloads;

public sealed class CommentReportedAuditPayload
{
    public string CommentReportPublicId { get; init; } = string.Empty;

    public string CommentPublicId { get; init; } = string.Empty;

    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public long ReporterUserId { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    public string ReportStatus { get; init; } = string.Empty;

    public bool CreatedNewCase { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}