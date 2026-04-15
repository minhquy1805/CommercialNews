using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence.Read;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;

/// <summary>
/// Returns a detailed admin-facing view of a single email delivery,
/// including its attempt history for troubleshooting and operations.
/// This is a read-only use case, so it does not open a transaction.
/// </summary>
public sealed class GetEmailDeliveryByIdUseCase : IGetEmailDeliveryByIdUseCase
{
    private readonly IEmailDeliveryQueryRepository _emailDeliveryQueryRepository;

    public GetEmailDeliveryByIdUseCase(
        IEmailDeliveryQueryRepository emailDeliveryQueryRepository)
    {
        _emailDeliveryQueryRepository = emailDeliveryQueryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryQueryRepository));
    }

    public async Task<Result<GetEmailDeliveryByIdResponse>> ExecuteAsync(
        GetEmailDeliveryByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.EmailDeliveryId <= 0)
            {
                return Result<GetEmailDeliveryByIdResponse>.Failure(
                    NotificationsErrors.EmailDelivery.InvalidId);
            }

            EmailDeliveryDetailResult? detail =
                await _emailDeliveryQueryRepository.GetByIdAsync(
                    request.EmailDeliveryId,
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
            _ => NotificationsErrors.ValidationFailed
        };
    }
}