using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Transactions;
using Notifications.Application.Validation.EmailDeliveries.RetryEmailDelivery;
using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;

/// <summary>
/// Accepts a safe admin retry request for an existing email delivery.
/// This use case does not guarantee final delivery success.
/// It only validates retry eligibility and re-queues the delivery so the worker
/// can process it asynchronously later.
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
        Error? validationError = RetryEmailDeliveryValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<RetryEmailDeliveryResponse>.Failure(validationError);
        }

        try
        {
            var emailDelivery = await _emailDeliveryRepository.GetByIdAsync(
                request.EmailDeliveryId,
                cancellationToken);

            if (emailDelivery is null)
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.NotFound);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.AlreadySent);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Queued, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sending, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(emailDelivery.Status, EmailDeliveryStatus.Dead, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.RetryNotAllowed);
            }

            if (!string.Equals(emailDelivery.Status, EmailDeliveryStatus.Failed, StringComparison.OrdinalIgnoreCase))
            {
                return Result<RetryEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.InvalidState);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _emailDeliveryRepository.RequeueForRetryAsync(
                    request.EmailDeliveryId,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<RetryEmailDeliveryResponse>.Failure(
                        NotificationsErrors.Delivery.StaleWriteConflict);
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
            "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE_TRANSITION"
                => NotificationsErrors.Delivery.InvalidState,

            "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_ID"
                => NotificationsErrors.InvalidRequest,

            _ => NotificationsErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND"
                => NotificationsErrors.Delivery.NotFound,

            "NOTIFICATIONS.EMAIL_DELIVERY_STALE_WRITE_CONFLICT"
                => NotificationsErrors.Delivery.StaleWriteConflict,

            _ => NotificationsErrors.DependencyUnavailable
        };
    }
}