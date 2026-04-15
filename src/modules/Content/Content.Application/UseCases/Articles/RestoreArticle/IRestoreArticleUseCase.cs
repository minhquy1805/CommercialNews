using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.RestoreArticle
{
    public interface IRestoreArticleUseCase
    {
        Task<Result<RestoreArticleResponseDto>> ExecuteAsync(
            RestoreArticleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

