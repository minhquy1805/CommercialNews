namespace Identity.Application.Contracts.Ports
{
    public interface IRefreshTokenRevocationService
    {
        Task RevokeAllActiveByUserIdAsync(
            long userId,
            string? revokedReason,
            CancellationToken cancellationToken);
    }
}