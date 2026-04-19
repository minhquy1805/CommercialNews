using Authorization.Application.Contracts.RolePermissions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RolePermissions.GrantPermissionToRole;

public interface IGrantPermissionToRoleUseCase
{
    Task<Result<GrantPermissionToRoleResponseDto>> ExecuteAsync(
        GrantPermissionToRoleRequestDto request,
        CancellationToken cancellationToken = default);
}