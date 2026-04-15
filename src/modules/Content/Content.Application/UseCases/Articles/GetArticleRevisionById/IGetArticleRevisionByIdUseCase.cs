using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.GetArticleRevisionById
{
    public interface IGetArticleRevisionByIdUseCase
    {
        Task<Result<GetArticleRevisionByIdResponseDto>> ExecuteAsync(
            GetArticleRevisionByIdRequestDto request,
            CancellationToken cancellationToken = default);
    }
}

