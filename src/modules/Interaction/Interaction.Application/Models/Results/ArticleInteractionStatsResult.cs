namespace Interaction.Application.Models.Results;

public sealed record ArticleInteractionStatsResult(
    string ArticlePublicId,
    long ViewCount,
    long LikeCount,
    long VisibleCommentCount,
    long? ViewVersion,
    long StatsVersion,
    DateTime? LastMaterializedAtUtc,
    DateTime? LastPublishedAtUtc);