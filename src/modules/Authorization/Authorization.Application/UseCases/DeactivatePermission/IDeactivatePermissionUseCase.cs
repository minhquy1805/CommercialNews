using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.DeactivatePermission
{
    public interface IDeactivatePermissionUseCase
    {
        Task<Result<DeactivatePermissionResponseDto>> ExecuteAsync(
            DeactivatePermissionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}