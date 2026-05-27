namespace Interaction.Application.Models.Results;

public sealed record CommentModerationHistoryItemResult(
    string HistoryPublicId,
    string CommentPublicId,
    string? CommentModerationCasePublicId,
    string ActionType,
    string? FromStatus,
    string? ToStatus,
    long? ActorUserId,
    string ActorType,
    string? ReasonCode,
    string? Note,
    DateTime OccurredAtUtc,
    string? CorrelationId);