using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Domain.Entities;

public sealed class EmailDeliveryAttempt
{
    public long EmailDeliveryAttemptId { get; private set; }

    public long EmailDeliveryId { get; private set; }

    public string MessageId { get; private set; } = null!;

    public int AttemptNumber { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public string Outcome { get; private set; } = null!;

    public bool IsAmbiguous { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public string? ProviderErrorCode { get; private set; }

    public string? ErrorClass { get; private set; }

    public string? ErrorDetail { get; private set; }

    public string? CorrelationId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private EmailDeliveryAttempt()
    {
    }

    public static EmailDeliveryAttempt Start(
        long emailDeliveryId,
        string messageId,
        int attemptNumber,
        DateTime startedAt,
        string? correlationId = null)
    {
        ValidateEmailDeliveryId(emailDeliveryId);
        ValidateMessageId(messageId);
        ValidateAttemptNumber(attemptNumber);
        ValidateStartedAt(startedAt);
        ValidateCorrelationId(correlationId);

        return new EmailDeliveryAttempt
        {
            EmailDeliveryId = emailDeliveryId,
            MessageId = NormalizeRequired(messageId),
            AttemptNumber = attemptNumber,
            StartedAt = startedAt,
            Outcome = EmailAttemptOutcome.Skipped,
            IsAmbiguous = false,
            CorrelationId = NormalizeOptional(correlationId),
            CreatedAt = startedAt
        };
    }

    public static EmailDeliveryAttempt Rehydrate(
        long emailDeliveryAttemptId,
        long emailDeliveryId,
        string messageId,
        int attemptNumber,
        DateTime startedAt,
        DateTime? finishedAt,
        string outcome,
        bool isAmbiguous,
        string? providerMessageId,
        string? providerErrorCode,
        string? errorClass,
        string? errorDetail,
        string? correlationId,
        DateTime createdAt)
    {
        if (emailDeliveryAttemptId <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ID",
                "Email delivery attempt id must be greater than zero.");
        }

        ValidateEmailDeliveryId(emailDeliveryId);
        ValidateMessageId(messageId);
        ValidateAttemptNumber(attemptNumber);
        ValidateStartedAt(startedAt);
        ValidateFinishedAt(startedAt, finishedAt);
        ValidateOutcome(outcome);
        ValidateProviderMessageId(providerMessageId);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorClass(errorClass);
        ValidateErrorDetail(errorDetail);
        ValidateCorrelationId(correlationId);
        ValidateCreatedAt(createdAt, startedAt);

        return new EmailDeliveryAttempt
        {
            EmailDeliveryAttemptId = emailDeliveryAttemptId,
            EmailDeliveryId = emailDeliveryId,
            MessageId = NormalizeRequired(messageId),
            AttemptNumber = attemptNumber,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Outcome = NormalizeRequired(outcome),
            IsAmbiguous = isAmbiguous,
            ProviderMessageId = NormalizeOptional(providerMessageId),
            ProviderErrorCode = NormalizeOptional(providerErrorCode),
            ErrorClass = NormalizeOptional(errorClass),
            ErrorDetail = NormalizeOptional(errorDetail),
            CorrelationId = NormalizeOptional(correlationId),
            CreatedAt = createdAt
        };
    }

    public bool IsCompleted => FinishedAt is not null;

    public void CompleteAsSent(
        DateTime finishedAt,
        string? providerMessageId = null)
    {
        EnsureNotCompleted();
        ValidateFinishedAt(StartedAt, finishedAt);
        ValidateProviderMessageId(providerMessageId);

        Outcome = EmailAttemptOutcome.Sent;
        IsAmbiguous = false;
        FinishedAt = finishedAt;
        ProviderMessageId = NormalizeOptional(providerMessageId);
        ProviderErrorCode = null;
        ErrorClass = null;
        ErrorDetail = null;
    }

    public void CompleteAsFailed(
        DateTime finishedAt,
        string? providerErrorCode = null,
        string? errorClass = null,
        string? errorDetail = null)
    {
        EnsureNotCompleted();
        ValidateFinishedAt(StartedAt, finishedAt);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorClass(errorClass);
        ValidateErrorDetail(errorDetail);

        Outcome = EmailAttemptOutcome.Failed;
        IsAmbiguous = false;
        FinishedAt = finishedAt;
        ProviderErrorCode = NormalizeOptional(providerErrorCode);
        ErrorClass = NormalizeOptional(errorClass);
        ErrorDetail = NormalizeOptional(errorDetail);
    }

    public void CompleteAsTimeout(
        DateTime finishedAt,
        string? providerErrorCode = null,
        string? errorDetail = null,
        bool isAmbiguous = true)
    {
        EnsureNotCompleted();
        ValidateFinishedAt(StartedAt, finishedAt);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorDetail(errorDetail);

        Outcome = EmailAttemptOutcome.Timeout;
        IsAmbiguous = isAmbiguous;
        FinishedAt = finishedAt;
        ProviderErrorCode = NormalizeOptional(providerErrorCode);
        ErrorClass = isAmbiguous
            ? EmailErrorClass.Ambiguous
            : EmailErrorClass.Transient;
        ErrorDetail = NormalizeOptional(errorDetail);
    }

    public void CompleteAsSuppressed(
        DateTime finishedAt,
        string? providerErrorCode = null,
        string? errorDetail = null)
    {
        EnsureNotCompleted();
        ValidateFinishedAt(StartedAt, finishedAt);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorDetail(errorDetail);

        Outcome = EmailAttemptOutcome.Suppressed;
        IsAmbiguous = false;
        FinishedAt = finishedAt;
        ProviderErrorCode = NormalizeOptional(providerErrorCode);
        ErrorClass = EmailErrorClass.Policy;
        ErrorDetail = NormalizeOptional(errorDetail);
    }

    public void CompleteAsSkipped(
        DateTime finishedAt,
        string? errorDetail = null)
    {
        EnsureNotCompleted();
        ValidateFinishedAt(StartedAt, finishedAt);
        ValidateErrorDetail(errorDetail);

        Outcome = EmailAttemptOutcome.Skipped;
        IsAmbiguous = false;
        FinishedAt = finishedAt;
        ProviderErrorCode = null;
        ErrorClass = null;
        ErrorDetail = NormalizeOptional(errorDetail);
    }

    public void CompleteAsProviderRejected(
        DateTime finishedAt,
        string? providerMessageId = null,
        string? providerErrorCode = null,
        string? errorDetail = null)
    {
        EnsureNotCompleted();
        ValidateFinishedAt(StartedAt, finishedAt);
        ValidateProviderMessageId(providerMessageId);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorDetail(errorDetail);

        Outcome = EmailAttemptOutcome.ProviderRejected;
        IsAmbiguous = false;
        FinishedAt = finishedAt;
        ProviderMessageId = NormalizeOptional(providerMessageId);
        ProviderErrorCode = NormalizeOptional(providerErrorCode);
        ErrorClass = EmailErrorClass.Provider;
        ErrorDetail = NormalizeOptional(errorDetail);
    }

    private void EnsureNotCompleted()
    {
        if (FinishedAt is not null)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ALREADY_COMPLETED",
                "Email delivery attempt has already been completed.");
        }
    }

    private static void ValidateEmailDeliveryId(long emailDeliveryId)
    {
        if (emailDeliveryId <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_EMAIL_DELIVERY_ID",
                "Email delivery id must be greater than zero.");
        }
    }

    private static void ValidateMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_MESSAGE_ID_REQUIRED",
                "Message id is required.");
        }

        if (messageId.Trim().Length > 26)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_MESSAGE_ID_TOO_LONG",
                "Message id must not exceed 26 characters.");
        }
    }

    private static void ValidateAttemptNumber(int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ATTEMPT_NUMBER",
                "Attempt number must be greater than zero.");
        }
    }

    private static void ValidateStartedAt(DateTime startedAt)
    {
        if (startedAt == default)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_STARTED_AT_REQUIRED",
                "StartedAt is required.");
        }
    }

    private static void ValidateFinishedAt(DateTime startedAt, DateTime? finishedAt)
    {
        if (finishedAt is not null && finishedAt < startedAt)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_FINISHED_AT",
                "FinishedAt must be greater than or equal to StartedAt.");
        }
    }

    private static void ValidateOutcome(string? outcome)
    {
        if (!EmailAttemptOutcome.IsValid(outcome))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_OUTCOME_INVALID",
                "Attempt outcome is invalid.");
        }
    }

    private static void ValidateProviderMessageId(string? providerMessageId)
    {
        if (!string.IsNullOrWhiteSpace(providerMessageId) && providerMessageId.Trim().Length > 200)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_PROVIDER_MESSAGE_ID_TOO_LONG",
                "Provider message id must not exceed 200 characters.");
        }
    }

    private static void ValidateProviderErrorCode(string? providerErrorCode)
    {
        if (!string.IsNullOrWhiteSpace(providerErrorCode) && providerErrorCode.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_PROVIDER_ERROR_CODE_TOO_LONG",
                "Provider error code must not exceed 100 characters.");
        }
    }

    private static void ValidateErrorClass(string? errorClass)
    {
        if (!string.IsNullOrWhiteSpace(errorClass) && !EmailErrorClass.IsValid(errorClass))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_CLASS_INVALID",
                "Error class is invalid.");
        }
    }

    private static void ValidateErrorDetail(string? errorDetail)
    {
        if (!string.IsNullOrWhiteSpace(errorDetail) && errorDetail.Trim().Length > 2000)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_DETAIL_TOO_LONG",
                "Error detail must not exceed 2000 characters.");
        }
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId) && correlationId.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_CORRELATION_ID_TOO_LONG",
                "Correlation id must not exceed 100 characters.");
        }
    }

    private static void ValidateCreatedAt(DateTime createdAt, DateTime startedAt)
    {
        if (createdAt == default)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_CREATED_AT_REQUIRED",
                "CreatedAt is required.");
        }

        if (createdAt < startedAt)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_CREATED_AT_INVALID",
                "CreatedAt must be greater than or equal to StartedAt.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
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