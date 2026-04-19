using Authorization.Application.Contracts.RolePermissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RolePermissions.GetPermissionRoles;

public interface IGetPermissionRolesUseCase
{
    Task<Result<GetPermissionRolesResponseDto>> ExecuteAsync(
        GetPermissionRolesRequestDto request,
        CancellationToken cancellationToken = default);
}