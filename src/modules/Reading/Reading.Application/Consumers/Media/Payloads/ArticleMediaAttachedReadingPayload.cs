namespace Reading.Application.Consumers.Media.Payloads;

public sealed class ArticleMediaAttachedReadingPayload
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public string MediaPublicId { get; init; } = string.Empty;

    public long? ArticleMediaId { get; init; }

    public string Url { get; init; } = string.Empty;

    public string MediaType { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public string? AltTextOverride { get; init; }

    public string? EffectiveAltText { get; init; }

    public string? Caption { get; init; }

    public int SortOrder { get; init; }

    public bool IsPrimary { get; init; }

    public bool PrimaryChanged { get; init; }

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime AttachedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
