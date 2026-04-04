using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.RestoreArticleMedia;

public interface IRestoreArticleMediaUseCase
{
    Task<Result<RestoreArticleMediaResponse>> ExecuteAsync(
        RestoreArticleMediaRequest request,
        CancellationToken cancellationToken = default);
}