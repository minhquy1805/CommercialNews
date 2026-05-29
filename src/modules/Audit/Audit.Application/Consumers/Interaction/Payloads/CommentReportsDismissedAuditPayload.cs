namespace Audit.Application.Consumers.Interaction.Payloads;

public sealed class CommentReportsDismissedAuditPayload
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;

    public string CaseStatus { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    public long ModeratorUserId { get; init; }

    public DateTime ResolvedAtUtc { get; init; }
}