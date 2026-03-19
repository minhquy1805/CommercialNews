namespace Identity.Application.Contracts.Ports
{
    public interface IPasswordResetService
    {
        Task<long> ResetPasswordByTokenHashAsync(
            byte[] tokenHash,
            string newPasswordHash,
            CancellationToken cancellationToken);
    }
}

