using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace CommercialNews.BuildingBlocks.Outbox.Ports;

public interface IOutboxMessageRepository
{
    Task<long> InsertAsync(
        ISqlUnitOfWork unitOfWork,
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
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        CancellationToken cancellationToken = default);

    Task<int> MarkFailedAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        DateTime? nextRetryAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> MarkDeadAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default);

    Task<int> ResetToPendingAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        CancellationToken cancellationToken = default);
}