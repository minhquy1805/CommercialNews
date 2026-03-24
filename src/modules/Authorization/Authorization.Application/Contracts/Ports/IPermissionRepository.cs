using Authorization.Domain.Entities;

namespace Authorization.Application.Contracts.Ports
{
    public interface IPermissionRepository
    {
        Task<Permission?> GetByIdAsync(
            long permissionId,
            CancellationToken cancellationToken);
    }
}