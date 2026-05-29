using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Interaction.Payloads;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.UseCases.Projections.ApplyArticleInteractionCounterProjection;

namespace Reading.Application.Consumers.Interaction;

public sealed class InteractionReadingEventIngestionService
    : IInteractionReadingEventIngestionService
{
    private readonly IApplyArticleInteractionCounterProjectionUseCase
        _applyArticleInteractionCounterProjectionUseCase;

    public InteractionReadingEventIngestionService(
        IApplyArticleInteractionCounterProjectionUseCase
            applyArticleInteractionCounterProjectionUseCase)
    {
        _applyArticleInteractionCounterProjectionUseCase =
            applyArticleInteractionCounterProjectionUseCase
            ?? throw new ArgumentNullException(
                nameof(applyArticleInteractionCounterProjectionUseCase));
    }

    public Task<Result<ArticleProjectionApplyResult>>
        IngestArticleCountersProjectionPublishedAsync(
            InteractionReadingEnvelopeContext context,
            ArticleCountersProjectionPublishedReadingPayload payload,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new ApplyArticleInteractionCounterProjectionCommand(
            ArticlePublicId: payload.ArticlePublicId,
            ViewCount: payload.ViewCount,
            LikeCount: payload.LikeCount,
            VisibleCommentCount: payload.VisibleCommentCount,
            InteractionStatsVersion: payload.StatsVersion,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleInteractionCounterProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }
}