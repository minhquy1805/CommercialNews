using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.MarkArticleProjectionNotPublic;

public interface IMarkArticleProjectionNotPublicUseCase
{
    Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        MarkArticleProjectionNotPublicCommand command,
        CancellationToken cancellationToken = default);
}