using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence.Transactions;
using Notifications.Application.Ports.Persistence.Write;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxPublished;

/// <summary>
/// Marks a single outbox message as published after the notification runtime
/// has consumed it successfully and converted it into local delivery truth.
/// This is a write use case, so it uses a transaction boundary.
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
        try
        {
            if (request.OutboxMessageId <= 0)
            {
                return Result<MarkOutboxPublishedResponse>.Failure(
                    NotificationsErrors.OutboxMessage.InvalidId);
            }

            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<MarkOutboxPublishedResponse>.Failure(
                    NotificationsErrors.OutboxMessage.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _outboxMessageRepository.MarkPublishedAsync(
                    request.OutboxMessageId,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MarkOutboxPublishedResponse>.Failure(
                        NotificationsErrors.OutboxMessage.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<MarkOutboxPublishedResponse>.Success(
                    new MarkOutboxPublishedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = "Published"
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
        catch (NotificationsDomainException exception)
        {
            return Result<MarkOutboxPublishedResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(NotificationsDomainException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.OUTBOX_INVALID_ID" => NotificationsErrors.OutboxMessage.InvalidId,
            "NOTIFICATIONS.OUTBOX_INVALID_STATE_TRANSITION" => NotificationsErrors.OutboxMessage.InvalidStateTransition,
            _ => NotificationsErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.OUTBOX_MESSAGE_NOT_FOUND" => NotificationsErrors.OutboxMessage.NotFound,
            "NOTIFICATIONS.OUTBOX_STALE_WRITE_CONFLICT" => NotificationsErrors.OutboxMessage.StaleWriteConflict,
            _ => NotificationsErrors.ValidationFailed
        };
    }
}