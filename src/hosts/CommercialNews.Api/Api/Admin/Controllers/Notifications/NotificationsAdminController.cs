using CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Notifications.EmailDeliveries.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveries;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryAttempts;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryById;
using Notifications.Application.UseCases.EmailDeliveries.GetEmailDeliveryByMessageId;
using Notifications.Application.UseCases.EmailDeliveries.RetryEmailDelivery;

namespace CommercialNews.Api.Api.Admin.Controllers.Notifications;

[ApiController]
[Route("api/v1/admin/notifications")]
public sealed class NotificationsAdminController : ControllerBase
{
    private readonly IGetEmailDeliveriesUseCase _getEmailDeliveriesUseCase;
    private readonly IGetEmailDeliveryByIdUseCase _getEmailDeliveryByIdUseCase;
    private readonly IGetEmailDeliveryByMessageIdUseCase _getEmailDeliveryByMessageIdUseCase;
    private readonly IGetEmailDeliveryAttemptsUseCase _getEmailDeliveryAttemptsUseCase;
    private readonly IRetryEmailDeliveryUseCase _retryEmailDeliveryUseCase;

    public NotificationsAdminController(
        IGetEmailDeliveriesUseCase getEmailDeliveriesUseCase,
        IGetEmailDeliveryByIdUseCase getEmailDeliveryByIdUseCase,
        IGetEmailDeliveryByMessageIdUseCase getEmailDeliveryByMessageIdUseCase,
        IGetEmailDeliveryAttemptsUseCase getEmailDeliveryAttemptsUseCase,
        IRetryEmailDeliveryUseCase retryEmailDeliveryUseCase)
    {
        _getEmailDeliveriesUseCase = getEmailDeliveriesUseCase
            ?? throw new ArgumentNullException(nameof(getEmailDeliveriesUseCase));
        _getEmailDeliveryByIdUseCase = getEmailDeliveryByIdUseCase
            ?? throw new ArgumentNullException(nameof(getEmailDeliveryByIdUseCase));
        _getEmailDeliveryByMessageIdUseCase = getEmailDeliveryByMessageIdUseCase
            ?? throw new ArgumentNullException(nameof(getEmailDeliveryByMessageIdUseCase));
        _getEmailDeliveryAttemptsUseCase = getEmailDeliveryAttemptsUseCase
            ?? throw new ArgumentNullException(nameof(getEmailDeliveryAttemptsUseCase));
        _retryEmailDeliveryUseCase = retryEmailDeliveryUseCase
            ?? throw new ArgumentNullException(nameof(retryEmailDeliveryUseCase));
    }

    [HttpGet("email-deliveries")]
    [ProducesResponseType(typeof(GetEmailDeliveriesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEmailDeliveriesAsync(
        [FromQuery] GetEmailDeliveriesHttpRequest request,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetEmailDeliveriesRequest
        {
            Page = request.Page,
            PageSize = request.PageSize,
            FromCreatedAt = request.FromCreatedAt,
            ToCreatedAt = request.ToCreatedAt,
            RecipientUserId = request.RecipientUserId,
            ToEmailHash = request.ToEmailHash,
            TemplateKey = request.TemplateKey,
            Status = request.Status,
            CorrelationId = request.CorrelationId,
            MessageId = request.MessageId
        };

        Result<GetEmailDeliveriesResponse> result =
            await _getEmailDeliveriesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetEmailDeliveriesHttpResponse>.Failure(result.Error!));
        }

        GetEmailDeliveriesResponse value = result.Value!;

        var response = new GetEmailDeliveriesHttpResponse
        {
            Items = value.Items
                .Select(static item => new EmailDeliveryListItemHttpResponse
                {
                    EmailDeliveryId = item.EmailDeliveryId,
                    MessageId = item.MessageId,
                    RecipientUserId = item.RecipientUserId,
                    MaskedToEmail = item.MaskedToEmail,
                    ToEmailHash = item.ToEmailHash,
                    TemplateKey = item.TemplateKey,
                    TemplateVersion = item.TemplateVersion,
                    Provider = item.Provider,
                    Status = item.Status,
                    AttemptCount = item.AttemptCount,
                    LastAttemptAt = item.LastAttemptAt,
                    NextRetryAt = item.NextRetryAt,
                    SentAt = item.SentAt,
                    FailedAt = item.FailedAt,
                    DeadAt = item.DeadAt,
                    SuppressedAt = item.SuppressedAt,
                    AmbiguousAt = item.AmbiguousAt,
                    LastErrorCode = item.LastErrorCode,
                    LastErrorClass = item.LastErrorClass,
                    CorrelationId = item.CorrelationId,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                })
                .ToArray(),
            PageInfo = new NotificationPageInfoHttpResponse
            {
                Page = value.Page,
                PageSize = value.PageSize,
                TotalItems = value.TotalItems
            }
        };

        return this.ToActionResult(
            Result<GetEmailDeliveriesHttpResponse>.Success(response));
    }

    [HttpGet("email-deliveries/{emailDeliveryId:long}")]
    [ProducesResponseType(typeof(GetEmailDeliveryByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailDeliveryByIdAsync(
        [FromRoute] long emailDeliveryId,
        CancellationToken cancellationToken)
    {
        Result<GetEmailDeliveryByIdResponse> result =
            await _getEmailDeliveryByIdUseCase.ExecuteAsync(
                new GetEmailDeliveryByIdRequest
                {
                    EmailDeliveryId = emailDeliveryId
                },
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetEmailDeliveryByIdHttpResponse>.Failure(result.Error!));
        }

        GetEmailDeliveryByIdResponse value = result.Value!;

        var response = MapDetailResponse(value);

        return this.ToActionResult(
            Result<GetEmailDeliveryByIdHttpResponse>.Success(response));
    }

    [HttpGet("email-deliveries/by-message/{messageId}")]
    [ProducesResponseType(typeof(GetEmailDeliveryByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailDeliveryByMessageIdAsync(
        [FromRoute] string messageId,
        CancellationToken cancellationToken)
    {
        Result<GetEmailDeliveryByIdResponse> result =
            await _getEmailDeliveryByMessageIdUseCase.ExecuteAsync(
                new GetEmailDeliveryByMessageIdRequest
                {
                    MessageId = messageId
                },
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetEmailDeliveryByIdHttpResponse>.Failure(result.Error!));
        }

        GetEmailDeliveryByIdResponse value = result.Value!;

        var response = MapDetailResponse(value);

        return this.ToActionResult(
            Result<GetEmailDeliveryByIdHttpResponse>.Success(response));
    }

    [HttpGet("email-deliveries/{emailDeliveryId:long}/attempts")]
    [ProducesResponseType(typeof(GetEmailDeliveryAttemptsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailDeliveryAttemptsAsync(
        [FromRoute] long emailDeliveryId,
        CancellationToken cancellationToken)
    {
        Result<GetEmailDeliveryAttemptsResponse> result =
            await _getEmailDeliveryAttemptsUseCase.ExecuteAsync(
                new GetEmailDeliveryAttemptsRequest
                {
                    EmailDeliveryId = emailDeliveryId
                },
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetEmailDeliveryAttemptsHttpResponse>.Failure(result.Error!));
        }

        GetEmailDeliveryAttemptsResponse value = result.Value!;

        var response = new GetEmailDeliveryAttemptsHttpResponse
        {
            EmailDeliveryId = value.EmailDeliveryId,
            Items = value.Items
                .Select(static attempt => new EmailDeliveryAttemptHttpResponse
                {
                    EmailDeliveryAttemptId = attempt.EmailDeliveryAttemptId,
                    EmailDeliveryId = attempt.EmailDeliveryId,
                    AttemptNumber = attempt.AttemptNumber,
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt,
                    Outcome = attempt.Outcome,
                    IsAmbiguous = attempt.IsAmbiguous,
                    ProviderMessageId = attempt.ProviderMessageId,
                    ProviderErrorCode = attempt.ProviderErrorCode,
                    ErrorClass = attempt.ErrorClass,
                    ErrorDetail = attempt.ErrorDetail,
                    CorrelationId = attempt.CorrelationId,
                    CreatedAt = attempt.CreatedAt
                })
                .ToArray()
        };

        return this.ToActionResult(
            Result<GetEmailDeliveryAttemptsHttpResponse>.Success(response));
    }

    [HttpPost("email-deliveries/{emailDeliveryId:long}:retry")]
    [ProducesResponseType(typeof(RetryEmailDeliveryHttpResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryEmailDeliveryAsync(
        [FromRoute] long emailDeliveryId,
        CancellationToken cancellationToken)
    {
        Result<RetryEmailDeliveryResponse> result =
            await _retryEmailDeliveryUseCase.ExecuteAsync(
                new RetryEmailDeliveryRequest
                {
                    EmailDeliveryId = emailDeliveryId
                },
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<RetryEmailDeliveryHttpResponse>.Failure(result.Error!));
        }

        var response = new RetryEmailDeliveryHttpResponse
        {
            EmailDeliveryId = result.Value!.EmailDeliveryId,
            MessageId = result.Value.MessageId,
            Status = result.Value.Status,
            Accepted = result.Value.Accepted
        };

        return StatusCode(StatusCodes.Status202Accepted, response);
    }

    private static GetEmailDeliveryByIdHttpResponse MapDetailResponse(
        GetEmailDeliveryByIdResponse value)
    {
        return new GetEmailDeliveryByIdHttpResponse
        {
            EmailDeliveryId = value.EmailDeliveryId,
            MessageId = value.MessageId,
            BusinessDedupeKey = value.BusinessDedupeKey,
            RecipientUserId = value.RecipientUserId,
            ToEmail = value.ToEmail,
            ToEmailHash = value.ToEmailHash,
            TemplateKey = value.TemplateKey,
            TemplateVersion = value.TemplateVersion,
            Subject = value.Subject,
            Provider = value.Provider,
            ProviderMessageId = value.ProviderMessageId,
            Status = value.Status,
            AttemptCount = value.AttemptCount,
            LastAttemptAt = value.LastAttemptAt,
            NextRetryAt = value.NextRetryAt,
            SentAt = value.SentAt,
            FailedAt = value.FailedAt,
            DeadAt = value.DeadAt,
            SuppressedAt = value.SuppressedAt,
            AmbiguousAt = value.AmbiguousAt,
            LastError = value.LastError,
            LastErrorCode = value.LastErrorCode,
            LastErrorClass = value.LastErrorClass,
            CorrelationId = value.CorrelationId,
            CreatedAt = value.CreatedAt,
            UpdatedAt = value.UpdatedAt,
            Attempts = value.Attempts
                .Select(static attempt => new EmailDeliveryAttemptHttpResponse
                {
                    EmailDeliveryAttemptId = attempt.EmailDeliveryAttemptId,
                    EmailDeliveryId = attempt.EmailDeliveryId,
                    AttemptNumber = attempt.AttemptNumber,
                    StartedAt = attempt.StartedAt,
                    FinishedAt = attempt.FinishedAt,
                    Outcome = attempt.Outcome,
                    IsAmbiguous = attempt.IsAmbiguous,
                    ProviderMessageId = attempt.ProviderMessageId,
                    ProviderErrorCode = attempt.ProviderErrorCode,
                    ErrorClass = attempt.ErrorClass,
                    ErrorDetail = attempt.ErrorDetail,
                    CorrelationId = attempt.CorrelationId,
                    CreatedAt = attempt.CreatedAt
                })
                .ToArray()
        };
    }
}