namespace Reading.Application.Consumers.Interaction.Payloads;

public sealed class ArticleCountersProjectionPublishedReadingPayload
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public long ViewCount { get; init; }

    public long LikeCount { get; init; }

    public long VisibleCommentCount { get; init; }

    public long StatsVersion { get; init; }

    public DateTime ProjectedAtUtc { get; init; }
}