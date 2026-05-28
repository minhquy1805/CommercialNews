namespace Audit.Application.Consumers.Interaction.Payloads;

public sealed class CommentDeletedByAuthorAuditPayload
{
    public string CommentPublicId { get; init; } = string.Empty;

    public string ArticlePublicId { get; init; } = string.Empty;

    public long AuthorUserId { get; init; }

    public bool WasVisible { get; init; }

    public bool ClosedOpenCase { get; init; }

    public DateTime DeletedAtUtc { get; init; }
}
