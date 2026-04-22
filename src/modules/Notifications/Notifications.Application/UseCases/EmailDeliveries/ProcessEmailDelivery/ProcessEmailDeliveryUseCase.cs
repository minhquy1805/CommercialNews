using System.Text.Json;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Contracts.Services;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Services;
using Notifications.Application.Ports.Transactions;
using Notifications.Application.Validation.EmailDeliveries.ProcessEmailDelivery;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.UseCases.EmailDeliveries.ProcessEmailDelivery;

/// <summary>
/// Processes a single existing email delivery inside the notification runtime.
/// It renders the template, sends the email, records an attempt, and updates
/// the delivery state safely. It does not make upstream business truth valid.
/// </summary>
public sealed class ProcessEmailDeliveryUseCase : IProcessEmailDeliveryUseCase
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly IEmailDeliveryAttemptRepository _emailDeliveryAttemptRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationTemplateRenderer _notificationTemplateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IProviderResultClassifier _providerResultClassifier;
    private readonly INotificationRetryPolicy _notificationRetryPolicy;

    public ProcessEmailDeliveryUseCase(
        IEmailDeliveryRepository emailDeliveryRepository,
        IEmailDeliveryAttemptRepository emailDeliveryAttemptRepository,
        INotificationsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        INotificationTemplateRenderer notificationTemplateRenderer,
        IEmailSender emailSender,
        IProviderResultClassifier providerResultClassifier,
        INotificationRetryPolicy notificationRetryPolicy)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
        _emailDeliveryAttemptRepository = emailDeliveryAttemptRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryAttemptRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationTemplateRenderer = notificationTemplateRenderer
            ?? throw new ArgumentNullException(nameof(notificationTemplateRenderer));
        _emailSender = emailSender
            ?? throw new ArgumentNullException(nameof(emailSender));
        _providerResultClassifier = providerResultClassifier
            ?? throw new ArgumentNullException(nameof(providerResultClassifier));
        _notificationRetryPolicy = notificationRetryPolicy
            ?? throw new ArgumentNullException(nameof(notificationRetryPolicy));
    }

    public async Task<Result<ProcessEmailDeliveryResponse>> ExecuteAsync(
        ProcessEmailDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ProcessEmailDeliveryValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ProcessEmailDeliveryResponse>.Failure(validationError);
        }

        try
        {
            EmailDelivery? emailDelivery = await _emailDeliveryRepository.GetByIdAsync(
                request.EmailDeliveryId,
                cancellationToken);

            if (emailDelivery is null)
            {
                return Result<ProcessEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.NotFound);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ProcessEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.AlreadySent);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Suppressed, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ProcessEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.InvalidState);
            }

            if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Dead, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ProcessEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.InvalidState);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            int attemptNumber = emailDelivery.AttemptCount + 1;

            string? effectiveCorrelationId = !string.IsNullOrWhiteSpace(request.CorrelationId)
                ? request.CorrelationId.Trim()
                : emailDelivery.CorrelationId;

            IReadOnlyDictionary<string, string> variables = DeserializeVariables(emailDelivery.VariablesJson);

            NotificationRenderResult renderResult =
                await _notificationTemplateRenderer.RenderAsync(
                    new NotificationRenderRequest
                    {
                        TemplateKey = emailDelivery.TemplateKey,
                        Variables = variables
                    },
                    cancellationToken);

            if (!renderResult.IsSuccess || string.IsNullOrWhiteSpace(renderResult.Body))
            {
                return await HandleRenderFailureAsync(
                    emailDelivery,
                    attemptNumber,
                    nowUtc,
                    effectiveCorrelationId,
                    renderResult,
                    cancellationToken);
            }

            EmailSendResult sendResult =
                await _emailSender.SendAsync(
                    new EmailSendRequest
                    {
                        ToEmail = emailDelivery.ToEmail,
                        TemplateKey = emailDelivery.TemplateKey,
                        Subject = renderResult.Subject,
                        Body = renderResult.Body,
                        CorrelationId = effectiveCorrelationId
                    },
                    cancellationToken);

            ProviderClassificationResult classification =
                _providerResultClassifier.Classify(sendResult);

            return await PersistProcessingOutcomeAsync(
                emailDelivery,
                attemptNumber,
                nowUtc,
                effectiveCorrelationId,
                sendResult,
                classification,
                cancellationToken);
        }
        catch (JsonException)
        {
            return Result<ProcessEmailDeliveryResponse>.Failure(
                NotificationsErrors.Delivery.InvalidState with
                {
                    Code = "NOTIFICATIONS.EMAIL_DELIVERY_VARIABLES_JSON_INVALID",
                    Message = "Email delivery variables payload is invalid."
                });
        }
        catch (PersistenceException exception)
        {
            return Result<ProcessEmailDeliveryResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (NotificationsDomainException exception)
        {
            return Result<ProcessEmailDeliveryResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task<Result<ProcessEmailDeliveryResponse>> HandleRenderFailureAsync(
        EmailDelivery emailDelivery,
        int attemptNumber,
        DateTime nowUtc,
        string? correlationId,
        NotificationRenderResult renderResult,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            EmailDeliveryAttempt attempt = EmailDeliveryAttempt.Start(
                emailDeliveryId: emailDelivery.EmailDeliveryId,
                messageId: emailDelivery.MessageId,
                attemptNumber: attemptNumber,
                startedAt: nowUtc,
                correlationId: correlationId);

            attempt.CompleteAsFailed(
                finishedAt: nowUtc,
                providerErrorCode: renderResult.ErrorCode,
                errorClass: EmailErrorClass.Template,
                errorDetail: renderResult.ErrorMessage);

            await _emailDeliveryAttemptRepository.InsertAsync(
                attempt,
                cancellationToken);

            int affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
                emailDelivery.EmailDeliveryId,
                renderResult.ErrorCode,
                EmailErrorClass.Template,
                cancellationToken);

            if (affectedRows <= 0)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<ProcessEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.StaleWriteConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProcessEmailDeliveryResponse>.Success(
                new ProcessEmailDeliveryResponse
                {
                    EmailDeliveryId = emailDelivery.EmailDeliveryId,
                    MessageId = emailDelivery.MessageId,
                    AttemptNumber = attemptNumber,
                    Status = EmailDeliveryStatus.Dead,
                    IsSuccess = false,
                    IsAmbiguous = false,
                    ProviderMessageId = null
                });
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result<ProcessEmailDeliveryResponse>> PersistProcessingOutcomeAsync(
        EmailDelivery emailDelivery,
        int attemptNumber,
        DateTime nowUtc,
        string? correlationId,
        EmailSendResult sendResult,
        ProviderClassificationResult classification,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            EmailDeliveryAttempt attempt = EmailDeliveryAttempt.Start(
                emailDeliveryId: emailDelivery.EmailDeliveryId,
                messageId: emailDelivery.MessageId,
                attemptNumber: attemptNumber,
                startedAt: nowUtc,
                correlationId: correlationId);

            if (classification.IsSuccess)
            {
                attempt.CompleteAsSent(
                    finishedAt: nowUtc,
                    providerMessageId: sendResult.ProviderMessageId);
            }
            else if (classification.IsAmbiguous)
            {
                attempt.CompleteAsTimeout(
                    finishedAt: nowUtc,
                    providerErrorCode: classification.ErrorCode,
                    errorDetail: classification.ErrorMessage,
                    isAmbiguous: true);
            }
            else if (string.Equals(classification.ErrorClass, EmailErrorClass.Policy, StringComparison.OrdinalIgnoreCase))
            {
                attempt.CompleteAsSuppressed(
                    finishedAt: nowUtc,
                    providerErrorCode: classification.ErrorCode,
                    errorDetail: classification.ErrorMessage);
            }
            else if (string.Equals(classification.ErrorClass, EmailErrorClass.Provider, StringComparison.OrdinalIgnoreCase))
            {
                attempt.CompleteAsProviderRejected(
                    finishedAt: nowUtc,
                    providerMessageId: sendResult.ProviderMessageId,
                    providerErrorCode: classification.ErrorCode,
                    errorDetail: classification.ErrorMessage);
            }
            else
            {
                attempt.CompleteAsFailed(
                    finishedAt: nowUtc,
                    providerErrorCode: classification.ErrorCode,
                    errorClass: classification.ErrorClass,
                    errorDetail: classification.ErrorMessage);
            }

            await _emailDeliveryAttemptRepository.InsertAsync(
                attempt,
                cancellationToken);

            int affectedRows;
            string finalStatus;

            if (classification.IsSuccess)
            {
                affectedRows = await _emailDeliveryRepository.MarkSentAsync(
                    emailDelivery.EmailDeliveryId,
                    cancellationToken);

                finalStatus = EmailDeliveryStatus.Sent;
            }
            else
            {
                RetryDecision retryDecision = _notificationRetryPolicy.Evaluate(
                    new NotificationRetryContext
                    {
                        TemplateKey = emailDelivery.TemplateKey,
                        CurrentStatus = emailDelivery.Status,
                        AttemptCount = attemptNumber,
                        ErrorClass = classification.ErrorClass,
                        ErrorCode = classification.ErrorCode,
                        IsAmbiguous = classification.IsAmbiguous,
                        NowUtc = nowUtc
                    });

                if (classification.IsAmbiguous)
                {
                    if (retryDecision.ShouldMarkDead)
                    {
                        affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
                            emailDelivery.EmailDeliveryId,
                            classification.ErrorCode,
                            classification.ErrorClass,
                            cancellationToken);

                        finalStatus = EmailDeliveryStatus.Dead;
                    }
                    else
                    {
                        affectedRows = await _emailDeliveryRepository.MarkAmbiguousAsync(
                            emailDelivery.EmailDeliveryId,
                            retryDecision.NextRetryAt,
                            classification.ErrorCode,
                            classification.ErrorClass,
                            cancellationToken);

                        finalStatus = EmailDeliveryStatus.Ambiguous;
                    }
                }
                else if (retryDecision.ShouldMarkDead)
                {
                    affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
                        emailDelivery.EmailDeliveryId,
                        classification.ErrorCode,
                        classification.ErrorClass,
                        cancellationToken);

                        finalStatus = EmailDeliveryStatus.Dead;
                }
                else if (retryDecision.ShouldRetry)
                {
                    affectedRows = await _emailDeliveryRepository.MarkFailedAsync(
                        emailDelivery.EmailDeliveryId,
                        retryDecision.NextRetryAt,
                        classification.ErrorCode,
                        classification.ErrorClass,
                        cancellationToken);

                    finalStatus = EmailDeliveryStatus.Failed;
                }
                else
                {
                    affectedRows = await _emailDeliveryRepository.MarkSuppressedAsync(
                        emailDelivery.EmailDeliveryId,
                        classification.ErrorCode,
                        classification.ErrorClass ?? EmailErrorClass.Policy,
                        cancellationToken);

                    finalStatus = EmailDeliveryStatus.Suppressed;
                }
            }

            if (affectedRows <= 0)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<ProcessEmailDeliveryResponse>.Failure(
                    NotificationsErrors.Delivery.StaleWriteConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ProcessEmailDeliveryResponse>.Success(
                new ProcessEmailDeliveryResponse
                {
                    EmailDeliveryId = emailDelivery.EmailDeliveryId,
                    MessageId = emailDelivery.MessageId,
                    AttemptNumber = attemptNumber,
                    Status = finalStatus,
                    IsSuccess = classification.IsSuccess,
                    IsAmbiguous = classification.IsAmbiguous,
                    ProviderMessageId = sendResult.ProviderMessageId
                });
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static IReadOnlyDictionary<string, string> DeserializeVariables(string variablesJson)
    {
        Dictionary<string, string>? variables = JsonSerializer.Deserialize<Dictionary<string, string>>(
            variablesJson,
            SerializerOptions);

        return variables is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(variables, StringComparer.OrdinalIgnoreCase);
    }

    private static Error MapDomainException(NotificationsDomainException exception)
    {
        return exception.Code switch
        {
            "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE_TRANSITION"
                => NotificationsErrors.Delivery.InvalidState,

            "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_ID"
                => NotificationsErrors.InvalidRequest,

            "NOTIFICATIONS.EMAIL_DELIVERY_VARIABLES_JSON_REQUIRED"
                => NotificationsErrors.ValidationFailed,

            "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_EMAIL_DELIVERY_ID"
                => NotificationsErrors.InvalidRequest,

            "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ATTEMPT_NUMBER"
                => NotificationsErrors.InvalidRequest,

            "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_OUTCOME_INVALID"
                => NotificationsErrors.ValidationFailed,

            "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_CLASS_INVALID"
                => NotificationsErrors.ValidationFailed,

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