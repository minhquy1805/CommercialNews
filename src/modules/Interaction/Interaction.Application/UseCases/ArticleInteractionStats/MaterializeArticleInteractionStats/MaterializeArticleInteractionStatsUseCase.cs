using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;
using Interaction.Application.Errors;
using Interaction.Application.Outbox.Payloads;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Application.Validation.ArticleInteractionStats;

namespace Interaction.Application.UseCases.ArticleInteractionStats.MaterializeArticleInteractionStats;

public sealed class MaterializeArticleInteractionStatsUseCase
    : IMaterializeArticleInteractionStatsUseCase
{
    private readonly IArticleInteractionStatsRepository _statsRepository;
    private readonly IInteractionUnitOfWork _unitOfWork;
    private readonly IInteractionOutboxWriter _outboxWriter;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public MaterializeArticleInteractionStatsUseCase(
        IArticleInteractionStatsRepository statsRepository,
        IInteractionUnitOfWork unitOfWork,
        IInteractionOutboxWriter outboxWriter,
        IPublicIdGenerator publicIdGenerator)
    {
        _statsRepository = statsRepository
            ?? throw new ArgumentNullException(nameof(statsRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<Result<MaterializeArticleInteractionStatsResponseDto>> ExecuteAsync(
        MaterializeArticleInteractionStatsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            MaterializeArticleInteractionStatsValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<MaterializeArticleInteractionStatsResponseDto>.Failure(
                validationError);
        }

        var articlePublicId =
            MaterializeArticleInteractionStatsValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        /*
         * This message id candidate is passed into the materialization procedure.
         *
         * If the public counter snapshot changes, the procedure persists this id
         * as LastPublishedMessageId, and the outbox record must use the same id.
         *
         * If the snapshot does not change, this candidate remains unused.
         */
        var publicationMessageIdCandidate = _publicIdGenerator.NewId();

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var materializeResult = await _statsRepository.MaterializeAsync(
                articlePublicId: articlePublicId,
                publicationMessageIdCandidate: publicationMessageIdCandidate,
                cancellationToken: cancellationToken);

            var stats = materializeResult.Stats;

            if (stats is null ||
                !string.Equals(
                    stats.ArticlePublicId,
                    articlePublicId,
                    StringComparison.OrdinalIgnoreCase) ||
                stats.StatsVersion < 1 ||
                !stats.LastMaterializedAtUtc.HasValue)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<MaterializeArticleInteractionStatsResponseDto>.Failure(
                    InteractionErrors.Counter.MaterializationFailed);
            }

            if (materializeResult.SnapshotChanged)
            {
                if (!stats.LastPublishedAtUtc.HasValue ||
                    !string.Equals(
                        stats.LastPublishedMessageId,
                        publicationMessageIdCandidate,
                        StringComparison.OrdinalIgnoreCase))
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MaterializeArticleInteractionStatsResponseDto>.Failure(
                        InteractionErrors.Counter.MaterializationFailed);
                }

                var payload = new ArticleCountersProjectionPublishedPayload(
                    ArticlePublicId: stats.ArticlePublicId,
                    ViewCount: stats.ViewCount,
                    LikeCount: stats.LikeCount,
                    VisibleCommentCount: stats.VisibleCommentCount,
                    StatsVersion: stats.StatsVersion,
                    ProjectedAtUtc: stats.LastPublishedAtUtc.Value);

                await _outboxWriter.WriteArticleCountersProjectionPublishedAsync(
                    messageId: publicationMessageIdCandidate,
                    aggregatePublicId: stats.ArticlePublicId,
                    aggregateVersion: stats.StatsVersion,
                    payload: payload,
                    correlationId: null,
                    initiatorUserId: null,
                    occurredAtUtc: stats.LastPublishedAtUtc.Value,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<MaterializeArticleInteractionStatsResponseDto>.Success(
                new MaterializeArticleInteractionStatsResponseDto
                {
                    ArticlePublicId = stats.ArticlePublicId,
                    ViewCount = stats.ViewCount,
                    LikeCount = stats.LikeCount,
                    VisibleCommentCount = stats.VisibleCommentCount,
                    StatsVersion = stats.StatsVersion,
                    LastMaterializedAtUtc = stats.LastMaterializedAtUtc.Value,
                    SnapshotChanged = materializeResult.SnapshotChanged
                });
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }
}