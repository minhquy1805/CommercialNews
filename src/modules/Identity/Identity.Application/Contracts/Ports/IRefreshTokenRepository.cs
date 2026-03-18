using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Ports
{
    public interface IRefreshTokenRepository
    {
        Task InsertAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken);
    }
}
