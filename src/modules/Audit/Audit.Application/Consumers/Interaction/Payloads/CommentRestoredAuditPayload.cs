namespace Audit.Application.Consumers.Interaction.Payloads;

public sealed class CommentRestoredAuditPayload
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long ModeratorUserId { get; init; }

    public DateTime RestoredAtUtc { get; init; }
}