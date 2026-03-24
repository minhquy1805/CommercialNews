namespace Authorization.Application.Contracts.Ports
{
    public interface IAuthorizationUserLookupService
    {
        Task<bool> ExistsAsync(
            long userId,
            CancellationToken cancellationToken);
    }
}