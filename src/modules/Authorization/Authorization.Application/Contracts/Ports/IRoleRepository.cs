using Authorization.Domain.Entities;

namespace Authorization.Application.Contracts.Ports
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(
            long roleId,
            CancellationToken cancellationToken);
    }
    
}

