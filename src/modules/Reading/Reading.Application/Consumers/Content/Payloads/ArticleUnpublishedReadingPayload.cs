namespace Reading.Application.Consumers.Content.Payloads;

public sealed class ArticleUnpublishedReadingPayload
{
    public long ArticleId { get; init; }

    public string ArticlePublicId { get; init; } = string.Empty;

    public string FromStatus { get; init; } = string.Empty;

    public string ToStatus { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public long ActorUserId { get; init; }

    public long Version { get; init; }

    public DateTime UnpublishedAtUtc { get; init; }

    public string BusinessDedupeKey { get; init; } = string.Empty;
}