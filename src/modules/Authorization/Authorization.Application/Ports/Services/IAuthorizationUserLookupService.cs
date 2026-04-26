using Authorization.Application.Models;

namespace Authorization.Application.Ports.Services;

public interface IAuthorizationUserLookupService
{
    Task<bool> ExistsAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationUserLookupResult?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}