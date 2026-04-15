using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Categories.UpdateCategory
{
    public interface IUpdateCategoryUseCase
    {
        Task<Result<UpdateCategoryResponseDto>> ExecuteAsync(
            UpdateCategoryRequestDto request,
            CancellationToken cancellationToken = default);
    }
}