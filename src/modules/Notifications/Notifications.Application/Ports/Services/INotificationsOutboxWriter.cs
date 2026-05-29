using Notifications.Application.Ports.Persistence;

namespace Notifications.Application.Ports.Services;

public interface INotificationsOutboxWriter
{
    Task<long> EnqueueEmailSentAsync(
        INotificationsUnitOfWork unitOfWork,
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        string? providerMessageId,
        string? correlationId,
        DateTime sentAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueEmailFailedAsync(
        INotificationsUnitOfWork unitOfWork,
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        DateTime? nextRetryAtUtc,
        string? lastErrorCode,
        string? lastErrorClass,
        bool isAmbiguous,
        string? correlationId,
        DateTime failedAtUtc,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueEmailDeadAsync(
        INotificationsUnitOfWork unitOfWork,
        long emailDeliveryId,
        long emailDeliveryAttemptId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string provider,
        int attemptCount,
        string? lastErrorCode,
        string? lastErrorClass,
        bool isAmbiguous,
        string? correlationId,
        DateTime deadAtUtc,
        CancellationToken cancellationToken = default);
}