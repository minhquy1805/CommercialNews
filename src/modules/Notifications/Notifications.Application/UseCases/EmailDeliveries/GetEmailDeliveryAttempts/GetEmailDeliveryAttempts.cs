using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveryAttempts;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;

/// <summary>
/// Returns attempt history for a single email delivery for admin troubleshooting and operations.
/// This is a read-only use case and does not open a transaction.
/// If the delivery does not exist, returns Delivery.NotFound.
/// If the delivery exists but has no attempts yet, returns an empty list.
/// </summary>
public sealed class GetEmailDeliveryAttemptsUseCase : IGetEmailDeliveryAttemptsUseCase
{
    private readonly IEmailDeliveryQueryRepository _emailDeliveryQueryRepository;

    public GetEmailDeliveryAttemptsUseCase(
        IEmailDeliveryQueryRepository emailDeliveryQueryRepository)
    {
        _emailDeliveryQueryRepository = emailDeliveryQueryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryQueryRepository));
    }

    public async Task<Result<GetEmailDeliveryAttemptsResponse>> ExecuteAsync(
        GetEmailDeliveryAttemptsRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = GetEmailDeliveryAttemptsValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetEmailDeliveryAttemptsResponse>.Failure(validationError);
        }

        try
        {
            EmailDeliveryDetailResult? delivery =
                await _emailDeliveryQueryRepository.GetByIdAsync(
                    request.EmailDeliveryId,
                    cancellationToken);

            if (delivery is null)
            {
                return Result<GetEmailDeliveryAttemptsResponse>.Failure(
                    NotificationsErrors.Delivery.NotFound);
            }

            IReadOnlyList<EmailDeliveryAttemptResultItem> attempts =
                await _emailDeliveryQueryRepository.GetAttemptsByEmailDeliveryIdAsync(
                    request.EmailDeliveryId,
                    cancellationToken);

            GetEmailDeliveryAttemptsResponse response = new()
            {
                EmailDeliveryId = request.EmailDeliveryId,
                Items = attempts
                    .Select(MapAttempt)
                    .ToArray()
            };

            return Result<GetEmailDeliveryAttemptsResponse>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<GetEmailDeliveryAttemptsResponse>.Failure(
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