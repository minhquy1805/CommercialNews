using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Contracts.ArticleInteractionStats.MaterializeArticleInteractionStats;

namespace Interaction.Application.Consumers.Stats;

public interface IInteractionStatsEventIngestionService
{
    Task<Result<MaterializeArticleInteractionStatsResponseDto>> IngestAsync(
        string articlePublicId,
        CancellationToken cancellationToken = default);
}
