using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.GetArticleById
{
    public interface IGetArticleByIdUseCase
    {
        Task<Result<GetArticleByIdResponseDto>> ExecuteAsync(
            GetArticleByIdRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

