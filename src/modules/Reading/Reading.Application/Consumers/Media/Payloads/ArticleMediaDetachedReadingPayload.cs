namespace Reading.Application.Consumers.Media.Payloads;

public sealed class ArticleMediaDetachedReadingPayload
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public bool PrimaryCleared { get; init; }

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime DetachedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
