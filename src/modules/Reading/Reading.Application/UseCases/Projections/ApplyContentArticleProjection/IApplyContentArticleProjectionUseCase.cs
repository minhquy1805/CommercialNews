using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.ApplyContentArticleProjection;

public interface IApplyContentArticleProjectionUseCase
{
    Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyContentArticleProjectionCommand command,
        CancellationToken cancellationToken = default);
}