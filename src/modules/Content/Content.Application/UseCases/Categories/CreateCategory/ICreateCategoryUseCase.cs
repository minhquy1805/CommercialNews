using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Categories.CreateCategory
{
    public interface ICreateCategoryUseCase
    {
        Task<Result<CreateCategoryResponseDto>> ExecuteAsync(
            CreateCategoryRequestDto request,
            CancellationToken cancellationToken = default);
    }
}