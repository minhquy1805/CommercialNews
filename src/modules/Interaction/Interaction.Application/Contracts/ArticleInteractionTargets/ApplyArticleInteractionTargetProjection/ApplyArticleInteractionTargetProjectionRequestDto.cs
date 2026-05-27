namespace Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

public sealed class ApplyArticleInteractionTargetProjectionRequestDto
{
    public string ArticlePublicId { get; init; } = string.Empty;

    public string SourceStatus { get; init; } = string.Empty;

    public bool IsInteractionEnabled { get; init; }

    public long SourceVersion { get; init; }

    public string SourceMessageId { get; init; } = string.Empty;

    public DateTime? SourceOccurredAtUtc { get; init; }
}