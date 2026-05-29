using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;
using Interaction.Application.Models.Results;
using Interaction.Application.Ports.Persistence;
using Interaction.Application.UseCases.ArticleInteractionStats.MaterializeArticleInteractionStats;
using Interaction.Application.Validation.ArticleInteractionStats;

namespace Interaction.Application.UseCases.ArticleInteractionStats.ProcessPendingViewStatsMaterialization;

public sealed class ProcessPendingViewStatsMaterializationUseCase
    : IProcessPendingViewStatsMaterializationUseCase
{
    private readonly IArticleViewCountRepository _articleViewCountRepository;
    private readonly IMaterializeArticleInteractionStatsUseCase _materializeArticleInteractionStatsUseCase;

    public ProcessPendingViewStatsMaterializationUseCase(
        IArticleViewCountRepository articleViewCountRepository,
        IMaterializeArticleInteractionStatsUseCase materializeArticleInteractionStatsUseCase)
    {
        _articleViewCountRepository = articleViewCountRepository
            ?? throw new ArgumentNullException(nameof(articleViewCountRepository));

        _materializeArticleInteractionStatsUseCase = materializeArticleInteractionStatsUseCase
            ?? throw new ArgumentNullException(nameof(materializeArticleInteractionStatsUseCase));
    }

    public async Task<Result<ProcessPendingViewStatsMaterializationResult>> ExecuteAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var validationError =
            ProcessPendingViewStatsMaterializationValidator.Validate(batchSize);

        if (validationError is not null)
        {
            return Result<ProcessPendingViewStatsMaterializationResult>.Failure(
                validationError);
        }

        IReadOnlyList<PendingViewStatsMaterializationItemResult> pendingItems =
            await _articleViewCountRepository.GetPendingStatsMaterializationBatchAsync(
                batchSize,
                cancellationToken);

        var materializedCount = 0;
        var unchangedCount = 0;
        var failedCount = 0;

        foreach (PendingViewStatsMaterializationItemResult pendingItem in pendingItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new MaterializeArticleInteractionStatsRequestDto
            {
                ArticlePublicId = pendingItem.ArticlePublicId
            };

            Result<MaterializeArticleInteractionStatsResponseDto> materializeResult =
                await _materializeArticleInteractionStatsUseCase.ExecuteAsync(
                    request,
                    cancellationToken);

            if (materializeResult.IsFailure)
            {
                failedCount++;
                continue;
            }

            if (materializeResult.Value!.SnapshotChanged)
            {
                materializedCount++;
            }
            else
            {
                unchangedCount++;
            }
        }

        return Result<ProcessPendingViewStatsMaterializationResult>.Success(
            new ProcessPendingViewStatsMaterializationResult(
                SelectedCount: pendingItems.Count,
                MaterializedCount: materializedCount,
                UnchangedCount: unchangedCount,
                FailedCount: failedCount));
    }
}