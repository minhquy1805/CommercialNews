using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Categories.RestoreCategory
{
    public interface IRestoreCategoryUseCase
    {
        Task<Result<RestoreCategoryResponseDto>> ExecuteAsync(
            RestoreCategoryRequestDto request,
            CancellationToken cancellationToken = default);
    }
}