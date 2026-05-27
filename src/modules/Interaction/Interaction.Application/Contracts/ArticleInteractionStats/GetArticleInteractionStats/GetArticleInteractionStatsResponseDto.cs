namespace Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats;

public sealed class GetArticleInteractionStatsResponseDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public long ViewCount { get; init; }

    public long LikeCount { get; init; }

    public long VisibleCommentCount { get; init; }

    public long StatsVersion { get; init; }

    public DateTime? LastMaterializedAtUtc { get; init; }

    public DateTime? LastPublishedAtUtc { get; init; }
}