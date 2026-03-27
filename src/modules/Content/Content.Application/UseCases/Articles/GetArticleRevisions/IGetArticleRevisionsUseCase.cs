using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.GetArticleRevisions
{
    public interface IGetArticleRevisionsUseCase
    {
        Task<Result<PagedResponse<ArticleRevisionListItemDto>>> ExecuteAsync(
            GetArticleRevisionsRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

