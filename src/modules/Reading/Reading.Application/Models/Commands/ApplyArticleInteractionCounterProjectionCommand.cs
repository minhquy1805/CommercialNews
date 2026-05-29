namespace Reading.Application.Models.Commands;

public sealed record ApplyArticleInteractionCounterProjectionCommand(
    string ArticlePublicId,
    long ViewCount,
    long LikeCount,
    long VisibleCommentCount,
    long InteractionStatsVersion,
    string MessageId,
    DateTime SourceOccurredAtUtc);