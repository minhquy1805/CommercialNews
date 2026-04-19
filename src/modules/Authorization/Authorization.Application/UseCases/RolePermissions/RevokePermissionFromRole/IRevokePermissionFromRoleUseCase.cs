using Authorization.Application.Contracts.RolePermissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RolePermissions.RevokePermissionFromRole;

public interface IRevokePermissionFromRoleUseCase
{
    Task<Result<RevokePermissionFromRoleResponseDto>> ExecuteAsync(
        RevokePermissionFromRoleRequestDto request,
        CancellationToken cancellationToken = default);
}