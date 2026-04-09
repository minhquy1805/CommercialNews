using Notifications.Domain.Entities;

namespace Notifications.Application.Ports.Persistence.Write;

public interface IOutboxMessageRepository
{
    Task<long> InsertAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);

    Task<OutboxMessage?> GetByIdAsync(
        long outboxMessageId,
        CancellationToken cancellationToken = default);

    Task<OutboxMessage?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int topN,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<int> MarkPublishedAsync(
        long outboxMessageId,
        CancellationToken cancellationToken = default);

    Task<int> MarkFailedAsync(
        long outboxMessageId,
        DateTime? nextRetryAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> MarkDeadLetterAsync(
        long outboxMessageId,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> ResetToPendingAsync(
        long outboxMessageId,
        CancellationToken cancellationToken = default);
}