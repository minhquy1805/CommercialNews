using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.ApplyAuthorProfileProjection;

public interface IApplyAuthorProfileProjectionUseCase
{
    Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyAuthorProfileProjectionCommand command,
        CancellationToken cancellationToken = default);
}
