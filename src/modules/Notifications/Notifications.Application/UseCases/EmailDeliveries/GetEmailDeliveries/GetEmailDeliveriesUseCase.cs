using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Validation.EmailDeliveries.GetEmailDeliveries;

namespace Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;

/// <summary>
/// Returns a paged admin-facing list of email deliveries for operations and troubleshooting.
/// This is a read-only use case and does not open a transaction.
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
        Error? validationError = GetEmailDeliveriesValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetEmailDeliveriesResponse>.Failure(validationError);
        }

        try
        {
            EmailDeliveryListQuery query = new()
            {
                Page = request.Page,
                PageSize = request.PageSize,
                FromCreatedAt = request.FromCreatedAt,
                ToCreatedAt = request.ToCreatedAt,
                RecipientUserId = request.RecipientUserId,
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
            MaskedToEmail = source.MaskedToEmail,
            TemplateKey = source.TemplateKey,
            Provider = source.Provider,
            Status = source.Status,
            AttemptCount = source.AttemptCount,
            LastAttemptAt = source.LastAttemptAt,
            NextRetryAt = source.NextRetryAt,
            SentAt = source.SentAt,
            LastErrorCode = source.LastErrorCode,
            LastErrorClass = source.LastErrorClass,
            CorrelationId = source.CorrelationId,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        _ = exception;

        return NotificationsErrors.DependencyUnavailable;
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