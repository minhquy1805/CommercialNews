namespace Audit.Infrastructure.Normalization.Interaction.EventPayloads;

internal sealed class CommentHiddenAuditPayload
{
    public string CommentPublicId { get; init; } = string.Empty;
    public string ArticlePublicId { get; init; } = string.Empty;
    public string ResolutionSource { get; init; } = string.Empty;
    public string? CommentModerationCasePublicId { get; init; }
    public long? ResolvedReportCount { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public long ModeratorUserId { get; init; }
    public DateTime HiddenAtUtc { get; init; }
}

internal sealed class CommentRestoredAuditPayload
{
    public string CommentPublicId { get; init; } = string.Empty;
    public string ArticlePublicId { get; init; } = string.Empty;
    public long ModeratorUserId { get; init; }
    public DateTime RestoredAtUtc { get; init; }
}

internal sealed class CommentDeletedByAuthorAuditPayload
{
    public string CommentPublicId { get; init; } = string.Empty;
    public string ArticlePublicId { get; init; } = string.Empty;
    public long AuthorUserId { get; init; }
    public bool WasVisible { get; init; }
    public bool ClosedOpenCase { get; init; }
    public DateTime DeletedAtUtc { get; init; }
}

internal sealed class CommentReportsDismissedAuditPayload
{
    public string CommentModerationCasePublicId { get; init; } = string.Empty;
    public string CaseStatus { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;
    public long ModeratorUserId { get; init; }
    public DateTime ResolvedAtUtc { get; init; }
}
