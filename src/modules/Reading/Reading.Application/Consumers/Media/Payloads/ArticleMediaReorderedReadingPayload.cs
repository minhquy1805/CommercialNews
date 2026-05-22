namespace Reading.Application.Consumers.Media.Payloads;

public sealed class ArticleMediaReorderedReadingPayload
{
    public long ArticleId { get; init; }

    public IReadOnlyCollection<ArticleMediaReorderedReadingItem> Items { get; init; }
        = Array.Empty<ArticleMediaReorderedReadingItem>();

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime ReorderedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}

public sealed class ArticleMediaReorderedReadingItem
{
    public long MediaId { get; init; }

    public int SortOrder { get; init; }
}
