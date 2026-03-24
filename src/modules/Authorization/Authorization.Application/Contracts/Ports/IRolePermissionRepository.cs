using Authorization.Application.Contracts.Queries;
using Authorization.Domain.Entities;

namespace Authorization.Application.Contracts.Ports
{
    public interface IRolePermissionRepository
    {
        Task<RolePermission?> GetActiveByRoleIdAndPermissionIdAsync(
            long roleId,
            long permissionId,
            CancellationToken cancellationToken);

        Task<RolePermission> InsertAsync(
            RolePermission rolePermission,
            CancellationToken cancellationToken);

        Task RevokeAsync(
            long roleId,
            long permissionId,
            long? revokedByUserId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<RolePermissionView>> GetActivePermissionsByRoleIdAsync(
            long roleId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<PermissionRoleView>> GetActiveRolesByPermissionIdAsync(
            long permissionId,
            CancellationToken cancellationToken);
    }
}