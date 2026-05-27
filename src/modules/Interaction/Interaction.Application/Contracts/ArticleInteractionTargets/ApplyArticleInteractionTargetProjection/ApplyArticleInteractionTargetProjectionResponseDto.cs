namespace Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

public sealed class ApplyArticleInteractionTargetProjectionResponseDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public string ApplyDecision { get; init; } = string.Empty;

    public string SourceStatus { get; init; } = string.Empty;

    public bool IsInteractionEnabled { get; init; }

    public long LastSourceVersion { get; init; }
}