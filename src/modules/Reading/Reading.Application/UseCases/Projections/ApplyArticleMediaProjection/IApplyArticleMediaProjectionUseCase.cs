using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;

namespace Reading.Application.UseCases.Projections.ApplyArticleMediaProjection;

public interface IApplyArticleMediaProjectionUseCase
{
    Task<Result<ArticleProjectionApplyResult>> UpsertAttachmentAsync(
        UpsertArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> SetPrimaryAsync(
        SetPrimaryArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> ReorderAsync(
        ReorderArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> DetachAsync(
        DetachArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default);
}
