using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.UnpublishArticle
{
    public interface IUnpublishArticleUseCase
    {
        Task<Result<UnpublishArticleResponseDto>> ExecuteAsync(
            UnpublishArticleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

