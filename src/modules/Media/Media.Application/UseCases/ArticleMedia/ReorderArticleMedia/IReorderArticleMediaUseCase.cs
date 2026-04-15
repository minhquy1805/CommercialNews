using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;

public interface IReorderArticleMediaUseCase
{
    Task<Result<ReorderArticleMediaResponse>> ExecuteAsync(
        ReorderArticleMediaRequest request,
        CancellationToken cancellationToken = default);
}