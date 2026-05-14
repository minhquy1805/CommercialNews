using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Categories.SoftDeleteCategory;

public interface ISoftDeleteCategoryUseCase
{
    Task<Result<SoftDeleteCategoryResponseDto>> ExecuteAsync(
        SoftDeleteCategoryRequestDto request,
        CancellationToken cancellationToken = default);
}