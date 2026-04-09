using Notifications.Application.Models.QueryModels;

namespace Notifications.Application.Ports.Persistence.Read;

public interface IOutboxMessageQueryRepository
{
    Task<OutboxMessageResult?> GetByIdAsync(
        long outboxMessageId,
        CancellationToken cancellationToken = default);

    Task<OutboxMessageResult?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessageResult>> GetByAggregateAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessageResult>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);
}