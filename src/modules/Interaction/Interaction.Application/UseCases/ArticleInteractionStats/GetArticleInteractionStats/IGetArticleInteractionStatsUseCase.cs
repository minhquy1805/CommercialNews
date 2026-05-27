using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.GetArticleInteractionStats;

namespace Interaction.Application.UseCases.ArticleInteractionStats.GetArticleInteractionStats;

public interface IGetArticleInteractionStatsUseCase
{
    Task<Result<GetArticleInteractionStatsResponseDto>> ExecuteAsync(
        GetArticleInteractionStatsRequestDto request,
        CancellationToken cancellationToken = default);
}