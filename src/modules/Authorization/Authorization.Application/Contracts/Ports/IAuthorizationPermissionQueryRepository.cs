using Authorization.Application.Contracts.Queries;

namespace Authorization.Application.Contracts.Ports
{
    public interface IAuthorizationPermissionQueryRepository
    {
        Task<IReadOnlyList<EffectivePermissionView>> GetEffectivePermissionsByUserIdAsync(
            long userId,
            CancellationToken cancellationToken);
    }
}