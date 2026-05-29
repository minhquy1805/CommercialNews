using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Models.Results;

namespace Interaction.Application.UseCases.ArticleInteractionStats
    .ProcessPendingViewStatsMaterialization;

public interface IProcessPendingViewStatsMaterializationUseCase
{
    Task<Result<ProcessPendingViewStatsMaterializationResult>> ExecuteAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}