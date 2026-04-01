using Authorization.Application.Models.QueryModels;
using Authorization.Domain.Entities;

namespace Authorization.Application.Ports.Persistence;

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetActiveByRoleIdAndPermissionIdAsync(
        long roleId,
        long permissionId,
        CancellationToken cancellationToken = default);

    Task<RolePermission> InsertAsync(
        RolePermission rolePermission,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        long roleId,
        long permissionId,
        long? revokedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolePermissionListResultItem>> GetActivePermissionsByRoleIdAsync(
        long roleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionRoleListResultItem>> GetActiveRolesByPermissionIdAsync(
        long permissionId,
        CancellationToken cancellationToken = default);
}