namespace Audit.Application.Consumers.Media.Payloads;

public sealed class ArticleMediaAttachedAuditPayload
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public long? ArticleMediaId { get; init; }

    public bool IsPrimary { get; init; }

    public bool PrimaryChanged { get; init; }

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime AttachedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}