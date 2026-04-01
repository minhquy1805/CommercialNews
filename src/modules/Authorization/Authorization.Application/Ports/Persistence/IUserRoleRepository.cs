using Authorization.Application.Models.QueryModels;
using Authorization.Domain.Entities;

namespace Authorization.Application.Ports.Persistence;

public interface IUserRoleRepository
{
    Task<UserRole?> GetActiveByUserIdAndRoleIdAsync(
        long userId,
        long roleId,
        CancellationToken cancellationToken = default);

    Task<UserRole> InsertAsync(
        UserRole userRole,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        long userId,
        long roleId,
        long? revokedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRoleListResultItem>> GetActiveRolesByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleUserListResultItem>> GetActiveUsersByRoleIdAsync(
        long roleId,
        CancellationToken cancellationToken = default);
}