using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Categories.DeleteCategory
{
    public interface IDeleteCategoryUseCase
    {
        Task<Result<DeleteCategoryResponseDto>> ExecuteAsync(
            DeleteCategoryRequestDto request,
            CancellationToken cancellationToken = default);
    }
}