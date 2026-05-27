namespace Interaction.Application.Models.Commands;

public sealed record ApplyArticleInteractionTargetProjectionCommand(
    string ArticlePublicId,
    string SourceStatus,
    bool IsInteractionEnabled,
    long SourceVersion,
    string SourceMessageId,
    DateTime? SourceOccurredAtUtc);