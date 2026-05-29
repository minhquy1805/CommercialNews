namespace Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats;

public sealed class GetArticleInteractionStatsRequestDto
{
    public string ArticlePublicId { get; init; } = string.Empty;
}