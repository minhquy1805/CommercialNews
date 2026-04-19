using Authorization.Application.Contracts.Permissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Permissions.ActivatePermission;

public interface IActivatePermissionUseCase
{
    Task<Result<ActivatePermissionResponseDto>> ExecuteAsync(
        ActivatePermissionRequestDto request,
        CancellationToken cancellationToken = default);
}