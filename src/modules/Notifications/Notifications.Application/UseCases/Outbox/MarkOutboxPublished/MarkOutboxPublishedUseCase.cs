using CommercialNews.BuildingBlocks.Outbox.Enums;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Transactions;
using Notifications.Application.Validation.Outbox.MarkOutboxPublished;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxPublished;

/// <summary>
/// Marks a single outbox message as published after successful downstream consumption.
/// This is a write use case and opens a transaction.
/// </summary>
public sealed class MarkOutboxPublishedUseCase : IMarkOutboxPublishedUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;

    public MarkOutboxPublishedUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        INotificationsUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<MarkOutboxPublishedResponse>> ExecuteAsync(
        MarkOutboxPublishedRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = MarkOutboxPublishedValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<MarkOutboxPublishedResponse>.Failure(validationError);
        }

        try
        {
            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<MarkOutboxPublishedResponse>.Failure(
                    NotificationsErrors.Outbox.NotFound);
            }

            if (string.Equals(outboxMessage.Status, OutboxMessageStatus.Published, StringComparison.OrdinalIgnoreCase))
            {
                return Result<MarkOutboxPublishedResponse>.Success(
                    new MarkOutboxPublishedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = outboxMessage.Status
                    });
            }

            if (string.Equals(outboxMessage.Status, OutboxMessageStatus.Dead, StringComparison.OrdinalIgnoreCase))
            {
                return Result<MarkOutboxPublishedResponse>.Failure(
                    NotificationsErrors.Outbox.InvalidState);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _outboxMessageRepository.MarkPublishedAsync(
                    _unitOfWork,
                    outboxMessage.OutboxMessageId,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MarkOutboxPublishedResponse>.Failure(
                        NotificationsErrors.Outbox.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<MarkOutboxPublishedResponse>.Success(
                    new MarkOutboxPublishedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = OutboxMessageStatus.Published
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<MarkOutboxPublishedResponse>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "OUTBOX.MESSAGE_NOT_FOUND" => NotificationsErrors.Outbox.NotFound,
            "OUTBOX.MESSAGE_STALE_WRITE_CONFLICT" => NotificationsErrors.Outbox.StaleWriteConflict,
            _ => NotificationsErrors.DependencyUnavailable
        };
    }
}