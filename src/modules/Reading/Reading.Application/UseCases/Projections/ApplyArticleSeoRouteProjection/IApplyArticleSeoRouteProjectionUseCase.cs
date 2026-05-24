using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.ApplyArticleSeoRouteProjection;

public interface IApplyArticleSeoRouteProjectionUseCase
{
    Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyArticleSeoRouteProjectionCommand command,
        CancellationToken cancellationToken = default);
}