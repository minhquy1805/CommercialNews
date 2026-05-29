namespace Interaction.Application.Contracts.Comments.GetCommentModerationHistory;

public sealed class GetCommentModerationHistoryItemResponseDto
{
    public string HistoryPublicId { get; init; } = string.Empty;

    public string CommentPublicId { get; init; } = string.Empty;

    public string? CommentModerationCasePublicId { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string? FromStatus { get; init; }

    public string? ToStatus { get; init; }

    public long? ActorUserId { get; init; }

    public string ActorType { get; init; } = string.Empty;

    public string? ReasonCode { get; init; }

    public string? Note { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public string? CorrelationId { get; init; }
}