using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;

public interface IAttachMediaToArticleUseCase
{
    Task<Result<AttachMediaToArticleResponse>> ExecuteAsync(
        AttachMediaToArticleRequest request,
        CancellationToken cancellationToken = default);
}