using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.ApplyArticleSeoMetadataProjection;

public interface IApplyArticleSeoMetadataProjectionUseCase
{
    Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyArticleSeoMetadataProjectionCommand command,
        CancellationToken cancellationToken = default);
}