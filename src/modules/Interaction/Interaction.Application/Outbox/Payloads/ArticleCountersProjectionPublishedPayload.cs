namespace Interaction.Application.Outbox.Payloads;

public sealed record ArticleCountersProjectionPublishedPayload(
    string ArticlePublicId,
    long ViewCount,
    long LikeCount,
    long VisibleCommentCount,
    long StatsVersion,
    DateTime ProjectedAtUtc);