namespace Identity.Application.Contracts.Ports
{
    public interface IRefreshTokenRevocationService
    {
        Task RevokeAsync(
            long refreshTokenId,
            string? revokedReason,
            byte[]? replacedByTokenHash,
            CancellationToken cancellationToken);
        
        Task RevokeAllActiveByUserIdAsync(
            long userId,
            string? revokedReason,
            CancellationToken cancellationToken);
    }
}