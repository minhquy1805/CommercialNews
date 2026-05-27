using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;

namespace Interaction.Application.UseCases.ArticleInteractionStats.MaterializeArticleInteractionStats;

public interface IMaterializeArticleInteractionStatsUseCase
{
    Task<Result<MaterializeArticleInteractionStatsResponseDto>> ExecuteAsync(
        MaterializeArticleInteractionStatsRequestDto request,
        CancellationToken cancellationToken = default);
}