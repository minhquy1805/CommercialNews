using Authorization.Domain.Entities;

namespace Authorization.Application.Contracts.Ports
{
    public interface IPermissionRepository
    {
        Task<Permission?> GetByIdAsync(
            long permissionId,
            CancellationToken cancellationToken);

        Task<Permission?> GetByNameNormalizedAsync(
            string nameNormalized,
            CancellationToken cancellationToken);

        Task<Permission> InsertAsync(
            Permission permission,
            CancellationToken cancellationToken);

        Task<Permission> UpdateAsync(
            Permission permission,
            CancellationToken cancellationToken);
    }
}