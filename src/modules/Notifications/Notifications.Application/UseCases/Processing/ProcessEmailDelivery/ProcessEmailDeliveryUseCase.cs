// using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
// using CommercialNews.BuildingBlocks.SharedKernel.Results;
// using CommercialNews.BuildingBlocks.SharedKernel.Time;
// using Notifications.Application.Contracts.Processing.Requests;
// using Notifications.Application.Contracts.Processing.Responses;
// using Notifications.Application.Contracts.Services;
// using Notifications.Application.Errors;
// using Notifications.Application.Ports.Persistence.Transactions;
// using Notifications.Application.Ports.Persistence.Write;
// using Notifications.Application.Ports.Services;
// using Notifications.Domain.Entities;
// using Notifications.Domain.Enums;
// using Notifications.Domain.Exceptions;

// namespace Notifications.Application.UseCases.Processing.ProcessEmailDelivery;

// /// <summary>
// /// Processes a single existing email delivery inside the notification runtime.
// /// This use case renders the template, sends the email through the provider,
// /// classifies the provider result, records an attempt, and updates the delivery state safely.
// /// It does not make upstream business truth valid; it only advances notification delivery state.
// /// </summary>
// /// <summary>
// /// TODO (deferred):
// /// This runtime/internal processing use case is intentionally postponed.
// /// It will be refactored after:
// /// 1. Application service contract models are finalized
// /// 2. Notifications ports are stabilized
// /// 3. Core read/admin use cases are completed
// /// Do not treat this implementation as final.
// /// </summary>
// public sealed class ProcessEmailDeliveryUseCase : IProcessEmailDeliveryUseCase
// {
//     private readonly IEmailDeliveryRepository _emailDeliveryRepository;
//     private readonly IEmailDeliveryAttemptRepository _emailDeliveryAttemptRepository;
//     private readonly INotificationsUnitOfWork _unitOfWork;
//     private readonly IDateTimeProvider _dateTimeProvider;
//     private readonly INotificationTemplateRenderer _notificationTemplateRenderer;
//     private readonly IEmailSender _emailSender;
//     private readonly IProviderResultClassifier _providerResultClassifier;
//     private readonly INotificationRetryPolicy _notificationRetryPolicy;

//     public ProcessEmailDeliveryUseCase(
//         IEmailDeliveryRepository emailDeliveryRepository,
//         IEmailDeliveryAttemptRepository emailDeliveryAttemptRepository,
//         INotificationsUnitOfWork unitOfWork,
//         IDateTimeProvider dateTimeProvider,
//         INotificationTemplateRenderer notificationTemplateRenderer,
//         IEmailSender emailSender,
//         IProviderResultClassifier providerResultClassifier,
//         INotificationRetryPolicy notificationRetryPolicy)
//     {
//         _emailDeliveryRepository = emailDeliveryRepository
//             ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
//         _emailDeliveryAttemptRepository = emailDeliveryAttemptRepository
//             ?? throw new ArgumentNullException(nameof(emailDeliveryAttemptRepository));
//         _unitOfWork = unitOfWork
//             ?? throw new ArgumentNullException(nameof(unitOfWork));
//         _dateTimeProvider = dateTimeProvider
//             ?? throw new ArgumentNullException(nameof(dateTimeProvider));
//         _notificationTemplateRenderer = notificationTemplateRenderer
//             ?? throw new ArgumentNullException(nameof(notificationTemplateRenderer));
//         _emailSender = emailSender
//             ?? throw new ArgumentNullException(nameof(emailSender));
//         _providerResultClassifier = providerResultClassifier
//             ?? throw new ArgumentNullException(nameof(providerResultClassifier));
//         _notificationRetryPolicy = notificationRetryPolicy
//             ?? throw new ArgumentNullException(nameof(notificationRetryPolicy));
//     }

//     public async Task<Result<ProcessEmailDeliveryResponse>> ExecuteAsync(
//         ProcessEmailDeliveryRequest request,
//         CancellationToken cancellationToken = default)
//     {
//         try
//         {
//             if (request.EmailDeliveryId <= 0)
//             {
//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.InvalidId);
//             }

//             if (!string.IsNullOrWhiteSpace(request.CorrelationId) &&
//                 request.CorrelationId.Trim().Length > 100)
//             {
//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.CorrelationIdTooLong);
//             }

//             EmailDelivery? emailDelivery = await _emailDeliveryRepository.GetByIdAsync(
//                 request.EmailDeliveryId,
//                 cancellationToken);

//             if (emailDelivery is null)
//             {
//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.NotFound);
//             }

//             if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase))
//             {
//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.AlreadySent);
//             }

//             if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Dead, StringComparison.OrdinalIgnoreCase))
//             {
//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.AlreadyDead);
//             }

//             if (string.Equals(emailDelivery.Status, EmailDeliveryStatus.Suppressed, StringComparison.OrdinalIgnoreCase))
//             {
//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.AlreadySuppressed);
//             }

//             DateTime nowUtc = _dateTimeProvider.UtcNow;
//             int attemptNumber = emailDelivery.AttemptCount + 1;
//             string effectiveCorrelationId = !string.IsNullOrWhiteSpace(request.CorrelationId)
//                 ? request.CorrelationId.Trim()
//                 : emailDelivery.CorrelationId ?? string.Empty;

//             NotificationRenderResult renderResult =
//                 await _notificationTemplateRenderer.RenderAsync(
//                     new NotificationRenderRequest
//                     {
//                         TemplateKey = emailDelivery.TemplateKey,
//                         Variables = request.Variables
//                     },
//                     cancellationToken);

//             if (!renderResult.IsSuccess || string.IsNullOrWhiteSpace(renderResult.Body))
//             {
//                 return await HandleRenderFailureAsync(
//                     emailDelivery,
//                     attemptNumber,
//                     nowUtc,
//                     effectiveCorrelationId,
//                     renderResult,
//                     cancellationToken);
//             }

//             EmailSendResult sendResult =
//                 await _emailSender.SendAsync(
//                     new EmailSendRequest
//                     {
//                         ToEmail = emailDelivery.ToEmail,
//                         TemplateKey = emailDelivery.TemplateKey,
//                         Subject = renderResult.Subject ?? emailDelivery.Subject,
//                         Body = renderResult.Body,
//                         CorrelationId = effectiveCorrelationId
//                     },
//                     cancellationToken);

//             ProviderClassificationResult providerClassification =
//                 _providerResultClassifier.Classify(sendResult);

//             return await PersistProcessingOutcomeAsync(
//                 emailDelivery,
//                 attemptNumber,
//                 nowUtc,
//                 effectiveCorrelationId,
//                 sendResult,
//                 providerClassification,
//                 cancellationToken);
//         }
//         catch (PersistenceException exception)
//         {
//             return Result<ProcessEmailDeliveryResponse>.Failure(
//                 MapPersistenceException(exception));
//         }
//         catch (NotificationsDomainException exception)
//         {
//             return Result<ProcessEmailDeliveryResponse>.Failure(
//                 MapDomainException(exception));
//         }
//     }

//     private async Task<Result<ProcessEmailDeliveryResponse>> HandleRenderFailureAsync(
//         EmailDelivery emailDelivery,
//         int attemptNumber,
//         DateTime nowUtc,
//         string correlationId,
//         NotificationRenderResult renderResult,
//         CancellationToken cancellationToken)
//     {
//         await _unitOfWork.BeginTransactionAsync(cancellationToken);

//         try
//         {
//             EmailDeliveryAttempt attempt = EmailDeliveryAttempt.Create(
//                 emailDeliveryId: emailDelivery.EmailDeliveryId,
//                 attemptNumber: attemptNumber,
//                 startedAt: nowUtc,
//                 outcome: EmailAttemptOutcome.Failed,
//                 isAmbiguous: false,
//                 finishedAt: nowUtc,
//                 providerMessageId: null,
//                 providerErrorCode: renderResult.ErrorCode,
//                 errorClass: EmailErrorClass.Template,
//                 errorDetail: renderResult.ErrorMessage,
//                 correlationId: correlationId);

//             await _emailDeliveryAttemptRepository.InsertAsync(
//                 attempt,
//                 cancellationToken);

//             int affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
//                 emailDelivery.EmailDeliveryId,
//                 renderResult.ErrorMessage,
//                 renderResult.ErrorCode,
//                 EmailErrorClass.Template,
//                 cancellationToken);

//             if (affectedRows <= 0)
//             {
//                 await _unitOfWork.RollbackAsync(cancellationToken);

//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.StaleWriteConflict);
//             }

//             await _unitOfWork.CommitAsync(cancellationToken);

//             return Result<ProcessEmailDeliveryResponse>.Success(
//                 new ProcessEmailDeliveryResponse
//                 {
//                     EmailDeliveryId = emailDelivery.EmailDeliveryId,
//                     MessageId = emailDelivery.MessageId,
//                     AttemptNumber = attemptNumber,
//                     Status = EmailDeliveryStatus.Dead,
//                     IsSuccess = false,
//                     IsAmbiguous = false,
//                     ProviderMessageId = null
//                 });
//         }
//         catch
//         {
//             await _unitOfWork.RollbackAsync(cancellationToken);
//             throw;
//         }
//     }

//     private async Task<Result<ProcessEmailDeliveryResponse>> PersistProcessingOutcomeAsync(
//         EmailDelivery emailDelivery,
//         int attemptNumber,
//         DateTime nowUtc,
//         string correlationId,
//         EmailSendResult sendResult,
//         ProviderClassificationResult providerClassification,
//         CancellationToken cancellationToken)
//     {
//         await _unitOfWork.BeginTransactionAsync(cancellationToken);

//         try
//         {
//             string outcome = ResolveAttemptOutcome(providerClassification, sendResult);

//             EmailDeliveryAttempt attempt = EmailDeliveryAttempt.Create(
//                 emailDeliveryId: emailDelivery.EmailDeliveryId,
//                 attemptNumber: attemptNumber,
//                 startedAt: nowUtc,
//                 outcome: outcome,
//                 isAmbiguous: providerClassification.IsAmbiguous,
//                 finishedAt: nowUtc,
//                 providerMessageId: sendResult.ProviderMessageId,
//                 providerErrorCode: providerClassification.ErrorCode,
//                 errorClass: providerClassification.ErrorClass,
//                 errorDetail: providerClassification.ErrorMessage,
//                 correlationId: correlationId);

//             await _emailDeliveryAttemptRepository.InsertAsync(
//                 attempt,
//                 cancellationToken);

//             int affectedRows;
//             string finalStatus;

//             if (providerClassification.IsSuccess)
//             {
//                 affectedRows = await _emailDeliveryRepository.MarkSentAsync(
//                     emailDelivery.EmailDeliveryId,
//                     sendResult.ProviderMessageId,
//                     cancellationToken);

//                 finalStatus = EmailDeliveryStatus.Sent;
//             }
//             else
//             {
//                 RetryDecision retryDecision = _notificationRetryPolicy.Evaluate(
//                     new NotificationRetryContext
//                     {
//                         TemplateKey = emailDelivery.TemplateKey,
//                         CurrentStatus = emailDelivery.Status,
//                         AttemptCount = attemptNumber,
//                         ErrorClass = providerClassification.ErrorClass,
//                         ErrorCode = providerClassification.ErrorCode,
//                         IsAmbiguous = providerClassification.IsAmbiguous,
//                         NowUtc = nowUtc
//                     });

//                 if (providerClassification.IsAmbiguous)
//                 {
//                     if (retryDecision.ShouldMarkDead)
//                     {
//                         affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
//                             emailDelivery.EmailDeliveryId,
//                             providerClassification.ErrorMessage,
//                             providerClassification.ErrorCode,
//                             providerClassification.ErrorClass,
//                             cancellationToken);

//                         finalStatus = EmailDeliveryStatus.Dead;
//                     }
//                     else
//                     {
//                         affectedRows = await _emailDeliveryRepository.MarkAmbiguousAsync(
//                             emailDelivery.EmailDeliveryId,
//                             retryDecision.NextRetryAt,
//                             providerClassification.ErrorMessage,
//                             providerClassification.ErrorCode,
//                             providerClassification.ErrorClass,
//                             cancellationToken);

//                         finalStatus = EmailDeliveryStatus.Ambiguous;
//                     }
//                 }
//                 else if (retryDecision.ShouldMarkDead)
//                 {
//                     affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
//                         emailDelivery.EmailDeliveryId,
//                         providerClassification.ErrorMessage,
//                         providerClassification.ErrorCode,
//                         providerClassification.ErrorClass,
//                         cancellationToken);

//                     finalStatus = EmailDeliveryStatus.Dead;
//                 }
//                 else if (retryDecision.ShouldRetry)
//                 {
//                     affectedRows = await _emailDeliveryRepository.MarkFailedAsync(
//                         emailDelivery.EmailDeliveryId,
//                         retryDecision.NextRetryAt,
//                         providerClassification.ErrorMessage,
//                         providerClassification.ErrorCode,
//                         providerClassification.ErrorClass,
//                         cancellationToken);

//                     finalStatus = EmailDeliveryStatus.Failed;
//                 }
//                 else
//                 {
//                     affectedRows = await _emailDeliveryRepository.MarkSuppressedAsync(
//                         emailDelivery.EmailDeliveryId,
//                         providerClassification.ErrorMessage,
//                         providerClassification.ErrorCode,
//                         providerClassification.ErrorClass ?? EmailErrorClass.Policy,
//                         cancellationToken);

//                     finalStatus = EmailDeliveryStatus.Suppressed;
//                 }
//             }

//             if (affectedRows <= 0)
//             {
//                 await _unitOfWork.RollbackAsync(cancellationToken);

//                 return Result<ProcessEmailDeliveryResponse>.Failure(
//                     NotificationsErrors.EmailDelivery.StaleWriteConflict);
//             }

//             await _unitOfWork.CommitAsync(cancellationToken);

//             return Result<ProcessEmailDeliveryResponse>.Success(
//                 new ProcessEmailDeliveryResponse
//                 {
//                     EmailDeliveryId = emailDelivery.EmailDeliveryId,
//                     MessageId = emailDelivery.MessageId,
//                     AttemptNumber = attemptNumber,
//                     Status = finalStatus,
//                     IsSuccess = providerClassification.IsSuccess,
//                     IsAmbiguous = providerClassification.IsAmbiguous,
//                     ProviderMessageId = sendResult.ProviderMessageId
//                 });
//         }
//         catch
//         {
//             await _unitOfWork.RollbackAsync(cancellationToken);
//             throw;
//         }
//     }

//     private static string ResolveAttemptOutcome(
//         ProviderClassificationResult providerClassification,
//         EmailSendResult sendResult)
//     {
//         if (providerClassification.IsSuccess)
//         {
//             return EmailAttemptOutcome.Sent;
//         }

//         if (providerClassification.IsAmbiguous)
//         {
//             return EmailAttemptOutcome.Timeout;
//         }

//         if (string.Equals(
//                 providerClassification.ErrorClass,
//                 EmailErrorClass.Policy,
//                 StringComparison.OrdinalIgnoreCase))
//         {
//             return EmailAttemptOutcome.Suppressed;
//         }

//         if (string.Equals(
//                 providerClassification.ErrorClass,
//                 EmailErrorClass.Provider,
//                 StringComparison.OrdinalIgnoreCase))
//         {
//             return EmailAttemptOutcome.ProviderRejected;
//         }

//         return EmailAttemptOutcome.Failed;
//     }

//     private static Error MapDomainException(NotificationsDomainException exception)
//     {
//         return exception.Code switch
//         {
//             "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_ID" => NotificationsErrors.EmailDelivery.InvalidId,
//             "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE_TRANSITION" => NotificationsErrors.EmailDelivery.InvalidStateTransition,
//             "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_EMAIL_DELIVERY_ID" => NotificationsErrors.EmailDeliveryAttempt.InvalidEmailDeliveryId,
//             "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ATTEMPT_NUMBER" => NotificationsErrors.EmailDeliveryAttempt.InvalidAttemptNumber,
//             "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_OUTCOME_INVALID" => NotificationsErrors.EmailDeliveryAttempt.OutcomeInvalid,
//             "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_CLASS_INVALID" => NotificationsErrors.EmailDeliveryAttempt.ErrorClassInvalid,
//             "NOTIFICATIONS.EMAIL_DELIVERY_CORRELATION_ID_TOO_LONG" => NotificationsErrors.EmailDelivery.CorrelationIdTooLong,
//             _ => NotificationsErrors.ValidationFailed
//         };
//     }

//     private static Error MapPersistenceException(PersistenceException exception)
//     {
//         return exception.Code switch
//         {
//             "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND" => NotificationsErrors.EmailDelivery.NotFound,
//             "NOTIFICATIONS.EMAIL_DELIVERY_STALE_WRITE_CONFLICT" => NotificationsErrors.EmailDelivery.StaleWriteConflict,
//             _ => NotificationsErrors.ValidationFailed
//         };
//     }
// }