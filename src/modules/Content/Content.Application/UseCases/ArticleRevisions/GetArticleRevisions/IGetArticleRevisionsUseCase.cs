using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.ArticleRevisions.GetArticleRevisions;

public interface IGetArticleRevisionsUseCase
{
    Task<Result<IReadOnlyList<ArticleRevisionItemDto>>> ExecuteAsync(
        GetArticleRevisionsRequestDto request,
        CancellationToken cancellationToken = default);
}