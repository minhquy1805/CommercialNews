using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Validation.Outbox.GetOutboxMessageById;

namespace Notifications.Application.UseCases.Outbox.GetOutboxMessageById;

/// <summary>
/// Returns a single outbox message for admin troubleshooting and runtime inspection.
/// This is a read-only use case and does not open a transaction.
/// </summary>
public sealed class GetOutboxMessageByIdUseCase : IGetOutboxMessageByIdUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public GetOutboxMessageByIdUseCase(
        IOutboxMessageRepository outboxMessageRepository)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
    }

    public async Task<Result<GetOutboxMessageByIdResponse>> ExecuteAsync(
        GetOutboxMessageByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetOutboxMessageByIdValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetOutboxMessageByIdResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<GetOutboxMessageByIdResponse>.Failure(
                    NotificationsErrors.Outbox.NotFound);
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
        catch (PersistenceException exception)
        {
            return Result<GetOutboxMessageByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "OUTBOX.MESSAGE_NOT_FOUND" => NotificationsErrors.Outbox.NotFound,
            _ => NotificationsErrors.DependencyUnavailable
        };
    }
}