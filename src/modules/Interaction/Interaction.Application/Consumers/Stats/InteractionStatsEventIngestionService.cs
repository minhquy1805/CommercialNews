using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;
using Interaction.Application.Errors;
using Interaction.Application.UseCases.ArticleInteractionStats.MaterializeArticleInteractionStats;

namespace Interaction.Application.Consumers.Stats;

public sealed class InteractionStatsEventIngestionService
    : IInteractionStatsEventIngestionService
{
    private readonly IMaterializeArticleInteractionStatsUseCase
        _materializeArticleInteractionStatsUseCase;

    public InteractionStatsEventIngestionService(
        IMaterializeArticleInteractionStatsUseCase
            materializeArticleInteractionStatsUseCase)
    {
        _materializeArticleInteractionStatsUseCase =
            materializeArticleInteractionStatsUseCase
            ?? throw new ArgumentNullException(
                nameof(materializeArticleInteractionStatsUseCase));
    }

    public Task<Result<MaterializeArticleInteractionStatsResponseDto>> IngestAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            return Task.FromResult(
                Result<MaterializeArticleInteractionStatsResponseDto>.Failure(
                    InteractionErrors.Article.ArticlePublicIdRequired));
        }

        var request = new MaterializeArticleInteractionStatsRequestDto
        {
            ArticlePublicId = articlePublicId.Trim()
        };

        return _materializeArticleInteractionStatsUseCase.ExecuteAsync(
            request,
            cancellationToken);
    }
}
