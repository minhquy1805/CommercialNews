namespace Identity.Application.Ports.Services;

public interface IIdentityOutboxWriter
{
    Task EnqueueVerificationEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string rawVerificationToken,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task EnqueuePasswordChangedEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task EnqueuePasswordResetEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string rawResetToken,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);
}