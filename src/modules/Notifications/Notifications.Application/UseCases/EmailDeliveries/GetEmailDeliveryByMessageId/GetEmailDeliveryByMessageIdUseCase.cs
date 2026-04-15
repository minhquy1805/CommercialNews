using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence.Read;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;

/// <summary>
/// Returns a detailed admin-facing view of a single email delivery by message id,
/// including attempt history for troubleshooting and operations.
/// This is a read-only use case, so it does not open a transaction.
/// </summary>
public sealed class GetEmailDeliveryByMessageIdUseCase : IGetEmailDeliveryByMessageIdUseCase
{
    private readonly IEmailDeliveryQueryRepository _emailDeliveryQueryRepository;

    public GetEmailDeliveryByMessageIdUseCase(
        IEmailDeliveryQueryRepository emailDeliveryQueryRepository)
    {
        _emailDeliveryQueryRepository = emailDeliveryQueryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryQueryRepository));
    }

    public async Task<Result<GetEmailDeliveryByIdResponse>> ExecuteAsync(
        GetEmailDeliveryByMessageIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.MessageId))
            {
                return Result<GetEmailDeliveryByIdResponse>.Failure(
                    NotificationsErrors.EmailDelivery.MessageIdRequired);
            }

            string messageId = request.MessageId.Trim();

            if (messageId.Length > 26)
            {
                return Result<GetEmailDeliveryByIdResponse>.Failure(
                    NotificationsErrors.EmailDelivery.MessageIdTooLong);
            }

            EmailDeliveryDetailResult? detail =
                await _emailDeliveryQueryRepository.GetByMessageIdAsync(
                    messageId,
                    cancellationToken);

            if (detail is null)
            {
                return Result<GetEmailDeliveryByIdResponse>.Failure(
                    NotificationsErrors.EmailDelivery.NotFound);
            }

            GetEmailDeliveryByIdResponse response = new()
            {
                EmailDeliveryId = detail.EmailDeliveryId,
                MessageId = detail.MessageId,
                BusinessDedupeKey = detail.BusinessDedupeKey,
                RecipientUserId = detail.RecipientUserId,
                ToEmail = detail.ToEmail,
                ToEmailHash = detail.ToEmailHash,
                TemplateKey = detail.TemplateKey,
                TemplateVersion = detail.TemplateVersion,
                Subject = detail.Subject,
                Provider = detail.Provider,
                ProviderMessageId = detail.ProviderMessageId,
                Status = detail.Status,
                AttemptCount = detail.AttemptCount,
                LastAttemptAt = detail.LastAttemptAt,
                NextRetryAt = detail.NextRetryAt,
                SentAt = detail.SentAt,
                FailedAt = detail.FailedAt,
                DeadAt = detail.DeadAt,
                SuppressedAt = detail.SuppressedAt,
                AmbiguousAt = detail.AmbiguousAt,
                LastError = detail.LastError,
                LastErrorCode = detail.LastErrorCode,
                LastErrorClass = detail.LastErrorClass,
                CorrelationId = detail.CorrelationId,
                CreatedAt = detail.CreatedAt,
                UpdatedAt = detail.UpdatedAt,
                Attempts = detail.Attempts
                    .Select(MapAttempt)
                    .ToArray()
            };

            return Result<GetEmailDeliveryByIdResponse>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<GetEmailDeliveryByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static EmailDeliveryAttemptItemResponse MapAttempt(
        EmailDeliveryAttemptResultItem source)
    {
        return new EmailDeliveryAttemptItemResponse
        {
            EmailDeliveryAttemptId = source.EmailDeliveryAttemptId,
            EmailDeliveryId = source.EmailDeliveryId,
            AttemptNumber = source.AttemptNumber,
            StartedAt = source.StartedAt,
            FinishedAt = source.FinishedAt,
            Outcome = source.Outcome,
            IsAmbiguous = source.IsAmbiguous,
            ProviderMessageId = source.ProviderMessageId,
            ProviderErrorCode = source.ProviderErrorCode,
            ErrorClass = source.ErrorClass,
            ErrorDetail = source.ErrorDetail,
            CorrelationId = source.CorrelationId,
            CreatedAt = source.CreatedAt
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND" => NotificationsErrors.EmailDelivery.NotFound,
            _ => NotificationsErrors.ValidationFailed
        };
    }
}