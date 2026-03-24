using Authorization.Domain.Entities;

namespace Authorization.Application.Contracts.Ports
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(
            long roleId,
            CancellationToken cancellationToken);

        Task<Role?> GetByNameNormalizedAsync(
            string nameNormalized,
            CancellationToken cancellationToken);

        Task<Role> InsertAsync(
            Role role,
            CancellationToken cancellationToken);

        Task<Role> UpdateAsync(
            Role role,
            CancellationToken cancellationToken);
    }
}