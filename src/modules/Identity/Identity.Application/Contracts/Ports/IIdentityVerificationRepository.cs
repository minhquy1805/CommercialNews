namespace Identity.Application.Contracts.Ports
{
    public interface IIdentityVerificationRepository
    {
        Task<long> VerifyEmailByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken);
    }
}
