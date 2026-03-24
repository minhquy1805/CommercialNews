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
    }
}