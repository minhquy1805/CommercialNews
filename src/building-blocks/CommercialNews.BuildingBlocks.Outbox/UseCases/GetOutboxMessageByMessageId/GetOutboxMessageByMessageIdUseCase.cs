using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Contracts.Responses;
using CommercialNews.BuildingBlocks.Outbox.Errors;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Outbox.Validation.GetOutboxMessageByMessageId;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.UseCases.GetOutboxMessageByMessageId;

/// <summary>
/// Returns a single outbox message by technical message id for admin troubleshooting.
/// This is a read-only use case and does not open a transaction.
/// </summary>
public sealed class GetOutboxMessageByMessageIdUseCase : IGetOutboxMessageByMessageIdUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public GetOutboxMessageByMessageIdUseCase(
        IOutboxMessageRepository outboxMessageRepository)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
    }

    public async Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByMessageIdRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetOutboxMessageByMessageIdValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetOutboxMessageByIdResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByMessageIdAsync(
                request.MessageId.Trim(),
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<GetOutboxMessageByIdResponse>.Failure(
                    OutboxErrors.Message.NotFound);
            }

            GetOutboxMessageByIdResponse response = new()
            {
                OutboxMessageId = outboxMessage.OutboxMessageId,
                MessageId = outboxMessage.MessageId,
                EventType = outboxMessage.EventType,
                AggregateType = outboxMessage.AggregateType,
                AggregateId = outboxMessage.AggregateId,
                AggregatePublicId = outboxMessage.AggregatePublicId,
                AggregateVersion = outboxMessage.AggregateVersion,
                Payload = outboxMessage.Payload,
                Headers = outboxMessage.Headers,
                CorrelationId = outboxMessage.CorrelationId,
                InitiatorUserId = outboxMessage.InitiatorUserId,
                Priority = outboxMessage.Priority,
                Status = outboxMessage.Status,
                AttemptCount = outboxMessage.AttemptCount,
                NextRetryAt = outboxMessage.NextRetryAt,
                LastAttemptAt = outboxMessage.LastAttemptAt,
                PublishedAt = outboxMessage.PublishedAt,
                LastError = outboxMessage.LastError,
                LastErrorCode = outboxMessage.LastErrorCode,
                LastErrorClass = outboxMessage.LastErrorClass,
                OccurredAt = outboxMessage.OccurredAt,
                CreatedAt = outboxMessage.CreatedAt,
                UpdatedAt = outboxMessage.UpdatedAt
            };

            return Result<GetOutboxMessageByIdResponse>.Success(response);
        }
        catch (PersistenceException)
        {
            return Result<GetOutboxMessageByIdResponse>.Failure(
                OutboxErrors.DependencyUnavailable);
        }
    }
}