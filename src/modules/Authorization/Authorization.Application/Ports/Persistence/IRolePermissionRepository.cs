using Authorization.Application.Models.QueryModels;
using Authorization.Domain.Entities;

namespace Authorization.Application.Ports.Persistence;

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetByRoleIdAndPermissionIdAsync(
        long roleId,
        long permissionId,
        CancellationToken cancellationToken = default);

    Task<RolePermission> InsertAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        long roleId,
        long permissionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolePermissionListResultItem>> GetPermissionsByRoleIdAsync(
        long roleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionRoleListResultItem>> GetRolesByPermissionIdAsync(
        long permissionId,
        CancellationToken cancellationToken = default);
}