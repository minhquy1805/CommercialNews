using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace CommercialNews.BuildingBlocks.Outbox.Persistence;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<long> WriteAsync(
        ISqlUnitOfWork unitOfWork,
        OutboxWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(request);

        DateTime occurredAtUtc = request.OccurredAtUtc == default
            ? _dateTimeProvider.UtcNow
            : request.OccurredAtUtc;

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: request.MessageId,
            eventType: request.EventType,
            aggregateType: request.AggregateType,
            aggregateId: request.AggregateId,
            payload: request.Payload,
            occurredAt: occurredAtUtc,
            aggregatePublicId: request.AggregatePublicId,
            aggregateVersion: request.AggregateVersion,
            headers: request.Headers,
            correlationId: request.CorrelationId,
            initiatorUserId: request.InitiatorUserId,
            priority: request.Priority);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
            outboxMessage,
            cancellationToken);
    }
}