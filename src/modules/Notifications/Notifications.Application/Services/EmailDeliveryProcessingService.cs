using System.Text.Json;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Notifications.Application.Contracts.Services;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Services;
using Notifications.Application.Ports.Transactions;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.Services;

public sealed class EmailDeliveryProcessingService : IEmailDeliveryProcessingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly IEmailDeliveryAttemptRepository _emailDeliveryAttemptRepository;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IEmailDeliveryRetryPolicy _retryPolicy;
    private readonly IProviderResultClassifier _providerResultClassifier;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationsOutboxWriter _outboxWriter;

    public EmailDeliveryProcessingService(
        IEmailDeliveryRepository emailDeliveryRepository,
        IEmailDeliveryAttemptRepository emailDeliveryAttemptRepository,
        IEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender,
        IEmailDeliveryRetryPolicy retryPolicy,
        IProviderResultClassifier providerResultClassifier,
        INotificationsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        INotificationsOutboxWriter outboxWriter)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));

        _emailDeliveryAttemptRepository = emailDeliveryAttemptRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryAttemptRepository));

        _templateRenderer = templateRenderer
            ?? throw new ArgumentNullException(nameof(templateRenderer));

        _emailSender = emailSender
            ?? throw new ArgumentNullException(nameof(emailSender));

        _retryPolicy = retryPolicy
            ?? throw new ArgumentNullException(nameof(retryPolicy));

        _providerResultClassifier = providerResultClassifier
            ?? throw new ArgumentNullException(nameof(providerResultClassifier));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));
    }

    public async Task<Result<int>> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            return Result<int>.Failure(NotificationsErrors.ValidationFailed);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            IReadOnlyList<EmailDelivery> deliveries =
                await ClaimPendingDeliveriesAsync(
                    batchSize,
                    nowUtc,
                    cancellationToken);

            int processedCount = 0;

            foreach (EmailDelivery delivery in deliveries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Result processResult = await ProcessClaimedDeliveryAsync(
                    delivery,
                    cancellationToken);

                if (processResult.IsSuccess)
                {
                    processedCount++;
                }
            }

            return Result<int>.Success(processedCount);
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure(NotificationsErrors.DependencyUnavailable);
        }
        catch (NotificationsDomainException)
        {
            return Result<int>.Failure(NotificationsErrors.ValidationFailed);
        }
    }

    private async Task<IReadOnlyList<EmailDelivery>> ClaimPendingDeliveriesAsync(
        int batchSize,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            IReadOnlyList<EmailDelivery> deliveries =
                await _emailDeliveryRepository.ClaimPendingAsync(
                    topN: batchSize,
                    nowUtc: nowUtc,
                    cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return deliveries;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result> ProcessClaimedDeliveryAsync(
        EmailDelivery delivery,
        CancellationToken cancellationToken)
    {
        EmailDeliveryAttempt attempt = await CreateStartedAttemptAsync(
            delivery,
            cancellationToken);

        EmailTemplateRenderResult renderResult = await RenderEmailAsync(
            delivery,
            cancellationToken);

        if (!renderResult.IsSuccess)
        {
            return await CompleteAsTemplateFailureAsync(
                delivery,
                attempt,
                renderResult,
                cancellationToken);
        }

        EmailSendResult sendResult = await _emailSender.SendAsync(
            new EmailSendRequest
            {
                MessageId = delivery.MessageId,
                ToEmail = delivery.ToEmail,
                TemplateKey = delivery.TemplateKey,
                Subject = renderResult.Subject,
                Body = renderResult.Body,
                CorrelationId = delivery.CorrelationId
            },
            cancellationToken);

        return await CompleteAfterSendAsync(
            delivery,
            attempt,
            sendResult,
            cancellationToken);
    }

    private async Task<EmailDeliveryAttempt> CreateStartedAttemptAsync(
        EmailDelivery delivery,
        CancellationToken cancellationToken)
    {
        DateTime startedAtUtc = _dateTimeProvider.UtcNow;

        EmailDeliveryAttempt attempt = EmailDeliveryAttempt.Start(
            emailDeliveryId: delivery.EmailDeliveryId,
            messageId: delivery.MessageId,
            attemptNumber: delivery.AttemptCount,
            startedAt: startedAtUtc,
            correlationId: delivery.CorrelationId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            long attemptId = await _emailDeliveryAttemptRepository.InsertAsync(
                attempt,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return EmailDeliveryAttempt.Rehydrate(
                emailDeliveryAttemptId: attemptId,
                emailDeliveryId: attempt.EmailDeliveryId,
                messageId: attempt.MessageId,
                attemptNumber: attempt.AttemptNumber,
                startedAt: attempt.StartedAt,
                finishedAt: attempt.FinishedAt,
                outcome: attempt.Outcome,
                isAmbiguous: attempt.IsAmbiguous,
                providerMessageId: attempt.ProviderMessageId,
                providerErrorCode: attempt.ProviderErrorCode,
                errorClass: attempt.ErrorClass,
                errorDetail: attempt.ErrorDetail,
                correlationId: attempt.CorrelationId,
                createdAt: attempt.CreatedAt);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<EmailTemplateRenderResult> RenderEmailAsync(
        EmailDelivery delivery,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> variables =
            DeserializeVariables(delivery.VariablesJson);

        return await _templateRenderer.RenderAsync(
            new EmailTemplateRenderRequest
            {
                TemplateKey = delivery.TemplateKey,
                Variables = variables
            },
            cancellationToken);
    }

    private async Task<Result> CompleteAsTemplateFailureAsync(
        EmailDelivery delivery,
        EmailDeliveryAttempt attempt,
        EmailTemplateRenderResult renderResult,
        CancellationToken cancellationToken)
    {
        DateTime finishedAtUtc = _dateTimeProvider.UtcNow;
        string? errorCode = NormalizeOptional(renderResult.ErrorCode);
        string? errorDetail = SanitizeErrorDetail(renderResult.ErrorMessage);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            attempt.CompleteAsFailed(
                finishedAt: finishedAtUtc,
                providerErrorCode: errorCode,
                errorClass: EmailErrorClass.Template,
                errorDetail: errorDetail);

            int attemptAffectedRows = await _emailDeliveryAttemptRepository.UpdateOutcomeAsync(
                attempt,
                cancellationToken);

            if (attemptAffectedRows <= 0)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure(NotificationsErrors.Delivery.StaleWriteConflict);
            }

            int deliveryAffectedRows = await _emailDeliveryRepository.MarkDeadAsync(
                emailDeliveryId: delivery.EmailDeliveryId,
                lastErrorCode: errorCode,
                lastErrorClass: EmailErrorClass.Template,
                cancellationToken: cancellationToken);

            if (deliveryAffectedRows <= 0)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure(NotificationsErrors.Delivery.StaleWriteConflict);
            }

            await _outboxWriter.EnqueueEmailDeadAsync(
                unitOfWork: _unitOfWork,
                emailDeliveryId: delivery.EmailDeliveryId,
                emailDeliveryAttemptId: attempt.EmailDeliveryAttemptId,
                messageId: delivery.MessageId,
                businessDedupeKey: delivery.BusinessDedupeKey,
                recipientUserId: delivery.RecipientUserId,
                toEmail: delivery.ToEmail,
                templateKey: delivery.TemplateKey,
                provider: delivery.Provider,
                attemptCount: delivery.AttemptCount,
                lastErrorCode: errorCode,
                lastErrorClass: EmailErrorClass.Template,
                isAmbiguous: attempt.IsAmbiguous,
                correlationId: delivery.CorrelationId,
                deadAtUtc: finishedAtUtc,
                cancellationToken: cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result> CompleteAfterSendAsync(
        EmailDelivery delivery,
        EmailDeliveryAttempt attempt,
        EmailSendResult sendResult,
        CancellationToken cancellationToken)
    {
        DateTime finishedAtUtc = _dateTimeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (sendResult.IsSuccess)
            {
                attempt.CompleteAsSucceeded(
                    finishedAt: finishedAtUtc,
                    providerMessageId: sendResult.ProviderMessageId);

                await _emailDeliveryAttemptRepository.UpdateOutcomeAsync(
                    attempt,
                    cancellationToken);

                await _emailDeliveryRepository.MarkSentAsync(
                    delivery.EmailDeliveryId,
                    cancellationToken);

                await _outboxWriter.EnqueueEmailSentAsync(
                    unitOfWork: _unitOfWork,
                    emailDeliveryId: delivery.EmailDeliveryId,
                    emailDeliveryAttemptId: attempt.EmailDeliveryAttemptId,
                    messageId: delivery.MessageId,
                    businessDedupeKey: delivery.BusinessDedupeKey,
                    recipientUserId: delivery.RecipientUserId,
                    toEmail: delivery.ToEmail,
                    templateKey: delivery.TemplateKey,
                    provider: delivery.Provider,
                    attemptCount: delivery.AttemptCount,
                    providerMessageId: sendResult.ProviderMessageId,
                    correlationId: delivery.CorrelationId,
                    sentAtUtc: finishedAtUtc,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            }

            string errorClass = DetermineErrorClass(sendResult);

            CompleteFailedAttempt(
                attempt,
                finishedAtUtc,
                sendResult,
                errorClass);

            await _emailDeliveryAttemptRepository.UpdateOutcomeAsync(
                attempt,
                cancellationToken);

            RetryDecision retryDecision = _retryPolicy.Evaluate(
                new EmailDeliveryRetryContext
                {
                    TemplateKey = delivery.TemplateKey,
                    CurrentStatus = delivery.Status,
                    AttemptCount = delivery.AttemptCount,
                    ErrorClass = errorClass,
                    ErrorCode = sendResult.ProviderErrorCode,
                    IsAmbiguous = sendResult.IsAmbiguous,
                    NowUtc = finishedAtUtc
                });

            if (retryDecision.ShouldMarkDead)
            {
                await _emailDeliveryRepository.MarkDeadAsync(
                    emailDeliveryId: delivery.EmailDeliveryId,
                    lastErrorCode: sendResult.ProviderErrorCode,
                    lastErrorClass: errorClass,
                    cancellationToken: cancellationToken);
            }
            if (retryDecision.ShouldRetry)
            {
                int affectedRows = await _emailDeliveryRepository.MarkFailedAsync(
                    emailDeliveryId: delivery.EmailDeliveryId,
                    nextRetryAt: retryDecision.NextRetryAt,
                    lastErrorCode: sendResult.ProviderErrorCode,
                    lastErrorClass: errorClass,
                    cancellationToken: cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result.Failure(NotificationsErrors.Delivery.StaleWriteConflict);
                }

                await _outboxWriter.EnqueueEmailFailedAsync(
                    unitOfWork: _unitOfWork,
                    emailDeliveryId: delivery.EmailDeliveryId,
                    emailDeliveryAttemptId: attempt.EmailDeliveryAttemptId,
                    messageId: delivery.MessageId,
                    businessDedupeKey: delivery.BusinessDedupeKey,
                    recipientUserId: delivery.RecipientUserId,
                    toEmail: delivery.ToEmail,
                    templateKey: delivery.TemplateKey,
                    provider: delivery.Provider,
                    attemptCount: delivery.AttemptCount,
                    nextRetryAtUtc: retryDecision.NextRetryAt,
                    lastErrorCode: sendResult.ProviderErrorCode,
                    lastErrorClass: errorClass,
                    isAmbiguous: attempt.IsAmbiguous,
                    correlationId: delivery.CorrelationId,
                    failedAtUtc: finishedAtUtc,
                    cancellationToken: cancellationToken);
            }
            else
            {
                int affectedRows = await _emailDeliveryRepository.MarkDeadAsync(
                    emailDeliveryId: delivery.EmailDeliveryId,
                    lastErrorCode: sendResult.ProviderErrorCode,
                    lastErrorClass: errorClass,
                    cancellationToken: cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result.Failure(NotificationsErrors.Delivery.StaleWriteConflict);
                }

                await _outboxWriter.EnqueueEmailDeadAsync(
                    unitOfWork: _unitOfWork,
                    emailDeliveryId: delivery.EmailDeliveryId,
                    emailDeliveryAttemptId: attempt.EmailDeliveryAttemptId,
                    messageId: delivery.MessageId,
                    businessDedupeKey: delivery.BusinessDedupeKey,
                    recipientUserId: delivery.RecipientUserId,
                    toEmail: delivery.ToEmail,
                    templateKey: delivery.TemplateKey,
                    provider: delivery.Provider,
                    attemptCount: delivery.AttemptCount,
                    lastErrorCode: sendResult.ProviderErrorCode,
                    lastErrorClass: errorClass,
                    isAmbiguous: attempt.IsAmbiguous,
                    correlationId: delivery.CorrelationId,
                    deadAtUtc: finishedAtUtc,
                    cancellationToken: cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private string DetermineErrorClass(EmailSendResult sendResult)
    {
        if (sendResult.IsAmbiguous)
        {
            return EmailErrorClass.Ambiguous;
        }

        ProviderClassificationResult classification =
            _providerResultClassifier.Classify(sendResult);

        return classification.ErrorClass!;
    }

    private static void CompleteFailedAttempt(
        EmailDeliveryAttempt attempt,
        DateTime finishedAtUtc,
        EmailSendResult sendResult,
        string errorClass)
    {
        string? errorDetail = SanitizeErrorDetail(sendResult.ProviderErrorMessage);

        if (sendResult.IsAmbiguous)
        {
            attempt.CompleteAsTimedOut(
                finishedAt: finishedAtUtc,
                providerErrorCode: sendResult.ProviderErrorCode,
                errorDetail: errorDetail,
                isAmbiguous: true);

            return;
        }

        if (EmailErrorClass.IsGenerallyTerminal(errorClass))
        {
            attempt.CompleteAsRejected(
                finishedAt: finishedAtUtc,
                providerMessageId: sendResult.ProviderMessageId,
                providerErrorCode: sendResult.ProviderErrorCode,
                errorDetail: errorDetail);

            return;
        }

        attempt.CompleteAsFailed(
            finishedAt: finishedAtUtc,
            providerErrorCode: sendResult.ProviderErrorCode,
            errorClass: errorClass,
            errorDetail: errorDetail);
    }

    private static IReadOnlyDictionary<string, string> DeserializeVariables(
        string variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        Dictionary<string, string>? variables =
            JsonSerializer.Deserialize<Dictionary<string, string>>(
                variablesJson,
                JsonOptions);

        return variables ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string? SanitizeErrorDetail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        return trimmed.Length <= 2000
            ? trimmed
            : trimmed[..2000];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}