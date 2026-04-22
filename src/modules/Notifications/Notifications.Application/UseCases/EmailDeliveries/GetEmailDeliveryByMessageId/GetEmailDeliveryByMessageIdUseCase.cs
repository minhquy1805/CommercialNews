using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveryByMessageId;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;

/// <summary>
/// Returns a detailed admin-facing view of a single email delivery by message id,
/// including attempt history for troubleshooting and operations.
/// This is a read-only use case and does not open a transaction.
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
        Error? validationError = GetEmailDeliveryByMessageIdValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetEmailDeliveryByIdResponse>.Failure(validationError);
        }

        try
        {
            string messageId = request.MessageId.Trim();

            EmailDeliveryDetailResult? detail =
                await _emailDeliveryQueryRepository.GetByMessageIdAsync(
                    messageId,
                    cancellationToken);

            if (detail is null)
            {
                return Result<GetEmailDeliveryByIdResponse>.Failure(
                    NotificationsErrors.Delivery.NotFound);
            }

            GetEmailDeliveryByIdResponse response = new()
            {
                EmailDeliveryId = detail.EmailDeliveryId,
                MessageId = detail.MessageId,
                BusinessDedupeKey = detail.BusinessDedupeKey,
                RecipientUserId = detail.RecipientUserId,
                ToEmail = detail.ToEmail,
                TemplateKey = detail.TemplateKey,
                Provider = detail.Provider,
                Status = detail.Status,
                AttemptCount = detail.AttemptCount,
                LastAttemptAt = detail.LastAttemptAt,
                NextRetryAt = detail.NextRetryAt,
                SentAt = detail.SentAt,
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
        _ = exception;

        return NotificationsErrors.DependencyUnavailable;
    }
}