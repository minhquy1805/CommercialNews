using Identity.Domain.Entities;

namespace Identity.Application.Ports.Persistence;

public interface IRefreshTokenRepository
{
    Task InsertAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetActiveByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        long refreshTokenId,
        DateTime revokedAtUtc,
        string? revokedReason,
        byte[]? replacedByTokenHash,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllActiveByUserIdAsync(
        long userId,
        DateTime revokedAtUtc,
        string? revokedReason,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenRotateResult?> RotateAsync(
        byte[] currentTokenHash,
        DateTime revokedAtUtc,
        string? revokedReason,
        byte[] newTokenHash,
        DateTime newCreatedAtUtc,
        DateTime newExpiresAtUtc,
        string? createdIp,
        string? userAgent,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
