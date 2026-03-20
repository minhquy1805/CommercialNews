namespace Identity.Application.Contracts.Ports
{
    public interface IRefreshTokenRotationService
    {
        Task<long> RotateAsync(
            byte[] currentTokenHash,
            byte[] newTokenHash,
            DateTime newExpiresAtUtc,
            string? createdIp,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken);
    }
}

