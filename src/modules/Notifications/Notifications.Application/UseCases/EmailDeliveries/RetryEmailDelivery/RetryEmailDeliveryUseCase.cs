using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence.Transactions;
using Notifications.Application.Ports.Persistence.Write;
using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;

/// <summary>
/// Handles a safe admin retry request for an existing email delivery.
/// This use case does not guarantee immediate final email success.
/// It only validates retry eligibility and re-queues the delivery safely
/// so that the worker can process it asynchronously later.
/// </summary>
public sealed class RetryEmailDeliveryUseCase : IRetryEmailDeliveryUseCase
{
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;

    public RetryEmailDeliveryUseCase(
        IEmailDeliveryRepository emailDeliveryRepository,
        INotificationsUnitOfWork unitOfWork)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<RetryEmailDeliveryResponse>> ExecuteAsync(
        RetryEmailDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.EmailDeliveryId <= 0)
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.EmailDelivery.InvalidId);
            }

            if (request.ActorUserId.HasValue && request.ActorUserId.Value <= 0)
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.ValidationFailed);
            }

            if (!string.IsNullOrWhiteSpace(request.CorrelationId) &&
                request.CorrelationId.Trim().Length > 100)
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.EmailDelivery.CorrelationIdTooLong);
            }

            var emailDelivery = await _emailDeliveryRepository.GetByIdAsync(
                request.EmailDeliveryId,
                cancellationToken);

            if (emailDelivery is null)
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.EmailDelivery.NotFound);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.EmailDelivery.AlreadySent);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Suppressed, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.EmailDelivery.AlreadySuppressed);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Queued, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sending, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.EmailDelivery.RetryNotAllowed);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _emailDeliveryRepository.ResetToQueuedAsync(
                    request.EmailDeliveryId,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<RetryEmailDeliveryResponse>.Failure(
                        NotificationsErrors.EmailDelivery.StaleWriteConflict);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<RetryEmailDeliveryResponse>.Success(
                    new RetryEmailDeliveryResponse
                    {
                        Accepted = true,
                        EmailDeliveryId = emailDelivery.EmailDeliveryId,
                        MessageId = emailDelivery.MessageId,
                        Status = EmailDeliveryStatus.Queued
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
            return Result<RetryEmailDeliveryResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (NotificationsDomainException exception)
        {
            return Result<RetryEmailDeliveryResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(NotificationsDomainException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_ID" => NotificationsErrors.EmailDelivery.InvalidId,
            "NOTIFICATIONS.EMAIL_DELIVERY_STATUS_INVALID" => NotificationsErrors.EmailDelivery.StatusInvalid,
            "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE_TRANSITION" => NotificationsErrors.EmailDelivery.InvalidStateTransition,
            "NOTIFICATIONS.EMAIL_DELIVERY_CORRELATION_ID_TOO_LONG" => NotificationsErrors.EmailDelivery.CorrelationIdTooLong,
            _ => NotificationsErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND" => NotificationsErrors.EmailDelivery.NotFound,
            "NOTIFICATIONS.EMAIL_DELIVERY_STALE_WRITE_CONFLICT" => NotificationsErrors.EmailDelivery.StaleWriteConflict,
            _ => NotificationsErrors.ValidationFailed
        };
    }
}