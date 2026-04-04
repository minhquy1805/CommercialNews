using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.DetachMediaFromArticle;

public interface IDetachMediaFromArticleUseCase
{
    Task<Result<DetachMediaFromArticleResponse>> ExecuteAsync(
        DetachMediaFromArticleRequest request,
        CancellationToken cancellationToken = default);
}