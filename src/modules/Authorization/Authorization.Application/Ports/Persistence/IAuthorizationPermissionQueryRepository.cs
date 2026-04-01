using Authorization.Application.Models.QueryModels;

namespace Authorization.Application.Ports.Persistence;

public interface IAuthorizationPermissionQueryRepository
{
    Task<IReadOnlyList<EffectivePermissionListResultItem>> GetEffectivePermissionsByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);
}