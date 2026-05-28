namespace Audit.Application.Consumers.Interaction.Payloads;

public sealed class CommentHiddenAuditPayload
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