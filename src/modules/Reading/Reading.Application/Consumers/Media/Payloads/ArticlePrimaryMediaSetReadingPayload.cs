namespace Reading.Application.Consumers.Media.Payloads;

public sealed class ArticlePrimaryMediaSetReadingPayload
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public string MediaPublicId { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string MediaType { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public string? AltTextOverride { get; init; }

    public string? EffectiveAltText { get; init; }

    public string? Caption { get; init; }

    public int SortOrder { get; init; }

    public long ActorUserId { get; init; }

    public long AttachmentSetVersion { get; init; }

    public DateTime PrimarySetAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}
