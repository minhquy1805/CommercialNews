using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence.Read;
using Notifications.Domain.Enums;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;

/// <summary>
/// Returns a paged admin-facing list of email deliveries for operations and troubleshooting.
/// This is a read-only use case, so it does not open a transaction.
/// It validates filter input, queries the read repository, and maps the result to the response contract.
/// </summary>
public sealed class GetEmailDeliveriesUseCase : IGetEmailDeliveriesUseCase
{
    private readonly IEmailDeliveryQueryRepository _emailDeliveryQueryRepository;

    public GetEmailDeliveriesUseCase(
        IEmailDeliveryQueryRepository emailDeliveryQueryRepository)
    {
        _emailDeliveryQueryRepository = emailDeliveryQueryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryQueryRepository));
    }

    public async Task<Result<GetEmailDeliveriesResponse>> ExecuteAsync(
        GetEmailDeliveriesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Page <= 0)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.ValidationFailed);
            }

            if (request.PageSize <= 0)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.ValidationFailed);
            }

            if (request.PageSize > 200)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.ValidationFailed);
            }

            if (request.RecipientUserId.HasValue && request.RecipientUserId.Value <= 0)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.EmailDelivery.RecipientUserIdInvalid);
            }

            if (!string.IsNullOrWhiteSpace(request.TemplateKey) &&
                !NotificationTemplateKey.IsValid(request.TemplateKey))
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.EmailDelivery.TemplateKeyInvalid);
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                !EmailDeliveryStatus.IsValid(request.Status))
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.EmailDelivery.StatusInvalid);
            }

            if (!string.IsNullOrWhiteSpace(request.ToEmailHash) &&
                request.ToEmailHash.Trim().Length > 64)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.EmailDelivery.ToEmailHashTooLong);
            }

            if (!string.IsNullOrWhiteSpace(request.CorrelationId) &&
                request.CorrelationId.Trim().Length > 100)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.EmailDelivery.CorrelationIdTooLong);
            }

            if (!string.IsNullOrWhiteSpace(request.MessageId) &&
                request.MessageId.Trim().Length > 26)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.EmailDelivery.MessageIdTooLong);
            }

            if (request.FromCreatedAt.HasValue &&
                request.ToCreatedAt.HasValue &&
                request.FromCreatedAt.Value >= request.ToCreatedAt.Value)
            {
                return Result<GetEmailDeliveriesResponse>.Failure(
                    NotificationsErrors.ValidationFailed);
            }

            EmailDeliveryListQuery query = new()
            {
                Page = request.Page,
                PageSize = request.PageSize,
                FromCreatedAt = request.FromCreatedAt,
                ToCreatedAt = request.ToCreatedAt,
                RecipientUserId = request.RecipientUserId,
                ToEmailHash = NormalizeOptional(request.ToEmailHash),
                TemplateKey = NormalizeOptional(request.TemplateKey),
                Status = NormalizeOptional(request.Status),
                CorrelationId = NormalizeOptional(request.CorrelationId),
                MessageId = NormalizeOptional(request.MessageId)
            };

            PagedQueryResult<EmailDeliveryListResultItem> pagedResult =
                await _emailDeliveryQueryRepository.SelectSkipAndTakeAsync(
                    query,
                    cancellationToken);

            GetEmailDeliveriesResponse response = new()
            {
                Items = pagedResult.Items
                    .Select(MapItem)
                    .ToArray(),
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalItems = pagedResult.TotalItems
            };

            return Result<GetEmailDeliveriesResponse>.Success(response);
        }
        catch (PersistenceException exception)
        {
            return Result<GetEmailDeliveriesResponse>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static EmailDeliveryListItemResponse MapItem(
        EmailDeliveryListResultItem source)
    {
        return new EmailDeliveryListItemResponse
        {
            EmailDeliveryId = source.EmailDeliveryId,
            MessageId = source.MessageId,
            RecipientUserId = source.RecipientUserId,
            ToEmail = source.ToEmail,
            TemplateKey = source.TemplateKey,
            TemplateVersion = source.TemplateVersion,
            Provider = source.Provider,
            Status = source.Status,
            AttemptCount = source.AttemptCount,
            LastAttemptAt = source.LastAttemptAt,
            NextRetryAt = source.NextRetryAt,
            SentAt = source.SentAt,
            FailedAt = source.FailedAt,
            DeadAt = source.DeadAt,
            SuppressedAt = source.SuppressedAt,
            AmbiguousAt = source.AmbiguousAt,
            LastErrorCode = source.LastErrorCode,
            LastErrorClass = source.LastErrorClass,
            CorrelationId = source.CorrelationId,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => NotificationsErrors.ValidationFailed
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}