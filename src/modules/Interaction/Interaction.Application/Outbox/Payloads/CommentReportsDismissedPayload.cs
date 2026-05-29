namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentReportsDismissedPayload(
    string CommentModerationCasePublicId,
    string CaseStatus,
    string ReasonCode,
    long ModeratorUserId,
    DateTime ResolvedAtUtc);