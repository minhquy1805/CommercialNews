using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.GetArticleMediaList;

public interface IGetArticleMediaListUseCase
{
    Task<Result<GetArticleMediaListResponse>> ExecuteAsync(
        GetArticleMediaListRequest request,
        CancellationToken cancellationToken = default);
}