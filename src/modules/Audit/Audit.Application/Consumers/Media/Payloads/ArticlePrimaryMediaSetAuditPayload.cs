namespace Audit.Application.Consumers.Media.Payloads;

public sealed class ArticlePrimaryMediaSetAuditPayload
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime PrimarySetAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}