using Authorization.Application.Contracts.Permissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Permissions.UpdatePermission;

public interface IUpdatePermissionUseCase
{
    Task<Result<UpdatePermissionResponseDto>> ExecuteAsync(
        UpdatePermissionRequestDto request,
        CancellationToken cancellationToken = default);
}