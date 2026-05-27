namespace Interaction.Application.Models.Results;

public sealed record ModerationCaseReportResult(
    string CommentReportPublicId,
    long ReporterUserId,
    string ReasonCode,
    string? Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);