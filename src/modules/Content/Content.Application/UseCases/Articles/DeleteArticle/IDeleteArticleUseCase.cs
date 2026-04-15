using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.DeleteArticle
{
    public interface IDeleteArticleUseCase
    {
        Task<Result<DeleteArticleResponseDto>> ExecuteAsync(
            DeleteArticleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

