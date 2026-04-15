using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Contracts.Outbox.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence.Transactions;
using Notifications.Application.Ports.Persistence.Write;
using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.UseCases.Outbox.MarkOutboxFailed;

/// <summary>
/// Marks a single outbox message as failed when notification processing
/// could not complete successfully but is still eligible for retry later.
/// This is a write use case, so it uses a transaction boundary.
/// </summary>
public sealed class MarkOutboxFailedUseCase : IMarkOutboxFailedUseCase
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;

    public MarkOutboxFailedUseCase(
        IOutboxMessageRepository outboxMessageRepository,
        INotificationsUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<MarkOutboxFailedResponse>> ExecuteAsync(
        MarkOutboxFailedRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.OutboxMessageId <= 0)
            {
                return Result<MarkOutboxFailedResponse>.Failure(
                    NotificationsErrors.OutboxMessage.InvalidId);
            }

            if (!string.IsNullOrWhiteSpace(request.LastErrorCode) &&
                request.LastErrorCode.Trim().Length > 100)
            {
                return Result<MarkOutboxFailedResponse>.Failure(
                    NotificationsErrors.OutboxMessage.ErrorCodeTooLong);
            }

            if (!string.IsNullOrWhiteSpace(request.LastErrorClass) &&
                !EmailErrorClass.IsValid(request.LastErrorClass))
            {
                return Result<MarkOutboxFailedResponse>.Failure(
                    NotificationsErrors.OutboxMessage.ErrorClassInvalid);
            }

            var outboxMessage = await _outboxMessageRepository.GetByIdAsync(
                request.OutboxMessageId,
                cancellationToken);

            if (outboxMessage is null)
            {
                return Result<MarkOutboxFailedResponse>.Failure(
                    NotificationsErrors.OutboxMessage.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _outboxMessageRepository.MarkFailedAsync(
                    request.OutboxMessageId,
                    request.NextRetryAt,
                    request.LastError,
                    request.LastErrorCode,
                    request.LastErrorClass,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<MarkOutboxFailedResponse>.Failure(
                        NotificationsErrors.OutboxMessage.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<MarkOutboxFailedResponse>.Success(
                    new MarkOutboxFailedResponse
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId,
                        MessageId = outboxMessage.MessageId,
                        Status = OutboxMessageStatus.Failed,
                        NextRetryAt = request.NextRetryAt
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
            return Result<MarkOutboxFailedResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (NotificationsDomainException exception)
        {
            return Result<MarkOutboxFailedResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(NotificationsDomainException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.OUTBOX_INVALID_ID" => NotificationsErrors.OutboxMessage.InvalidId,
            "NOTIFICATIONS.OUTBOX_INVALID_STATE_TRANSITION" => NotificationsErrors.OutboxMessage.InvalidStateTransition,
            "NOTIFICATIONS.OUTBOX_ERROR_CODE_TOO_LONG" => NotificationsErrors.OutboxMessage.ErrorCodeTooLong,
            "NOTIFICATIONS.OUTBOX_ERROR_CLASS_INVALID" => NotificationsErrors.OutboxMessage.ErrorClassInvalid,
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