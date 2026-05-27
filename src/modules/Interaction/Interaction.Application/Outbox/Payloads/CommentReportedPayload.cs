namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentReportedPayload(
    string CommentReportPublicId,
    string CommentPublicId,
    string CommentModerationCasePublicId,
    long ReporterUserId,
    string ReasonCode,
    string ReportStatus,
    bool CreatedNewCase,
    DateTime CreatedAtUtc);