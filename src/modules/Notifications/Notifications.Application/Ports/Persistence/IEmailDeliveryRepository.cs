using Notifications.Domain.Entities;

namespace Notifications.Application.Ports.Persistence;

public interface IEmailDeliveryRepository
{
    Task<long> InsertAsync(
        EmailDelivery emailDelivery,
        CancellationToken cancellationToken = default);

    Task<EmailDelivery?> GetByIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default);

    Task<EmailDelivery?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task<EmailDelivery?> GetByBusinessDedupeKeyAsync(
        string businessDedupeKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailDelivery>> ClaimPendingAsync(
        int topN,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<int> MarkSentAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default);

    Task<int> MarkFailedAsync(
        long emailDeliveryId,
        DateTime? nextRetryAt,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> MarkDeadAsync(
        long emailDeliveryId,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> MarkSuppressedAsync(
        long emailDeliveryId,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> MarkAmbiguousAsync(
        long emailDeliveryId,
        DateTime? nextRetryAt,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> ResetToQueuedAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default);
}
