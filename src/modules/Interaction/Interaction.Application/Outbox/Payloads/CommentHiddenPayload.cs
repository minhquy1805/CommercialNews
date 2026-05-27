namespace Interaction.Application.Outbox.Payloads;

public sealed record CommentHiddenPayload(
    string CommentPublicId,
    string ArticlePublicId,
    string ResolutionSource,
    string? CommentModerationCasePublicId,
    long? ResolvedReportCount,
    string ReasonCode,
    long ModeratorUserId,
    DateTime HiddenAtUtc);