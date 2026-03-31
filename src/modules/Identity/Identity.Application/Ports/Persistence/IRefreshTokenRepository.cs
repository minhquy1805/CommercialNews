using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence
{
    public interface IRefreshTokenRepository
    {
        Task InsertAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetActiveByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken = default);

        Task<bool> RevokeAsync(
            long refreshTokenId,
            string? revokedReason,
            byte[]? replacedByTokenHash,
            CancellationToken cancellationToken = default);

        Task<int> RevokeAllActiveByUserIdAsync(
            long userId,
            string? revokedReason,
            CancellationToken cancellationToken = default);

        Task<long?> RotateAsync(
            byte[] currentTokenHash,
            byte[] newTokenHash,
            DateTime newExpiresAtUtc,
            string? createdIp,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken = default);
    }
}