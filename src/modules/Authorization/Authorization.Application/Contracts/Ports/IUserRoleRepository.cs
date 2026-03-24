using Authorization.Application.Contracts.Queries;
using Authorization.Domain.Entities;

namespace Authorization.Application.Contracts.Ports
{
    public interface IUserRoleRepository
    {
        Task<UserRole?> GetActiveByUserIdAndRoleIdAsync(
            long userId,
            long roleId,
            CancellationToken cancellationToken);

        Task<UserRole> InsertAsync(
            UserRole userRole,
            CancellationToken cancellationToken);
        
        Task RevokeAsync(
            long userId,
            long roleId,
            long? revokedByUserId,
            CancellationToken cancellationToken);
        
        Task<IReadOnlyList<UserRoleView>> GetActiveRolesByUserIdAsync(
            long userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<RoleUserView>> GetActiveUsersByRoleIdAsync(
            long roleId,
            CancellationToken cancellationToken);
    }
}