namespace Identity.Application.Ports.Persistence
{
    public interface ILoginHistoryRepository
    {
        Task InsertAsync(
            long? userId,
            string? emailNormalizedAttempted,
            bool succeeded,
            string? failureReason,
            string? ipAddress,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken = default);
    }
}