namespace Interaction.Application.Models.Results;

public sealed record ModerationCaseReportDetailResult(
    string CommentReportPublicId,
    long ReporterUserId,
    string ReasonCode,
    string? Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);