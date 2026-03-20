using Identity.Domain.Entities;

namespace Identity.Application.Contracts.Ports
{
    public interface IRefreshTokenLookupService
    {
        Task<RefreshToken?> GetByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken);
    }
}