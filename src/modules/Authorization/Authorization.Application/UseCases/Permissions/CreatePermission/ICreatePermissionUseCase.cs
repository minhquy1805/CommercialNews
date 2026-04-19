using Authorization.Application.Contracts.Permissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Permissions.CreatePermission;

public interface ICreatePermissionUseCase
{
    Task<Result<CreatePermissionResponseDto>> ExecuteAsync(
        CreatePermissionRequestDto request,
        CancellationToken cancellationToken = default);
}