
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.GetArticleRevisions
{
    public interface IGetArticleRevisionsUseCase
    {
        Task<Result<PagedQueryResult<ArticleRevisionListItemDto>>> ExecuteAsync(
            GetArticleRevisionsRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

