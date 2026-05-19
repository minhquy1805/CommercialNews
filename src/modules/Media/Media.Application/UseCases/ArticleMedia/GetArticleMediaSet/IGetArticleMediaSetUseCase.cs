using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.GetArticleMediaSet;

public interface IGetArticleMediaSetUseCase
{
    Task<Result<GetArticleMediaSetResponse>> ExecuteAsync(
        GetArticleMediaSetRequest request,
        CancellationToken cancellationToken = default);
}