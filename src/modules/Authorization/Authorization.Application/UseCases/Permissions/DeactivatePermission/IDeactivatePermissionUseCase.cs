using Authorization.Application.Contracts.Permissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Permissions.DeactivatePermission;

public interface IDeactivatePermissionUseCase
{
    Task<Result<DeactivatePermissionResponseDto>> ExecuteAsync(
        DeactivatePermissionRequestDto request,
        CancellationToken cancellationToken = default);
}