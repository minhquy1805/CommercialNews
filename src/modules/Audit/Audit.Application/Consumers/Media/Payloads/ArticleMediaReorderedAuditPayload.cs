namespace Audit.Application.Consumers.Media.Payloads;

public sealed class ArticleMediaReorderedAuditPayload
{
    public long ArticleId { get; init; }

    public IReadOnlyCollection<ArticleMediaReorderedAuditItem> Items { get; init; }
        = Array.Empty<ArticleMediaReorderedAuditItem>();

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime ReorderedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

public sealed class ArticleMediaReorderedAuditItem
{
    public long MediaId { get; init; }

    public int SortOrder { get; init; }
}