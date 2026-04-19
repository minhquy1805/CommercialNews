using Authorization.Application.Contracts.RolePermissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RolePermissions.GetRolePermissions;

public interface IGetRolePermissionsUseCase
{
    Task<Result<GetRolePermissionsResponseDto>> ExecuteAsync(
        GetRolePermissionsRequestDto request,
        CancellationToken cancellationToken = default);
}