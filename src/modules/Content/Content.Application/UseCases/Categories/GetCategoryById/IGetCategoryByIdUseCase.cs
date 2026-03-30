using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Categories.GetCategoryById
{
    public interface IGetCategoryByIdUseCase
    {
        Task<Result<GetCategoryByIdResponseDto>> ExecuteAsync(
            GetCategoryByIdRequestDto request,
            CancellationToken cancellationToken = default);
    }
}