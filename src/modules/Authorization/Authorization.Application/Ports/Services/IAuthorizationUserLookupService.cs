namespace Authorization.Application.Ports.Services;

public interface IAuthorizationUserLookupService
{
    Task<bool> ExistsAsync(
        long userId,
        CancellationToken cancellationToken = default);
}