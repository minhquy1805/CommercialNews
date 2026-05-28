using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.ApplyArticleInteractionCounterProjection;

public interface IApplyArticleInteractionCounterProjectionUseCase
{
    Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyArticleInteractionCounterProjectionCommand command,
        CancellationToken cancellationToken = default);
}