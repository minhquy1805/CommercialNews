using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Domain.Entities;

public sealed class EmailDeliveryAttempt
{
    public long EmailDeliveryAttemptId { get; private set; }

    public long EmailDeliveryId { get; private set; }

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

    public static EmailDeliveryAttempt Create(
        long emailDeliveryId,
        int attemptNumber,
        DateTime startedAt,
        string outcome,
        bool isAmbiguous = false,
        DateTime? finishedAt = null,
        string? providerMessageId = null,
        string? providerErrorCode = null,
        string? errorClass = null,
        string? errorDetail = null,
        string? correlationId = null)
    {
        ValidateEmailDeliveryId(emailDeliveryId);
        ValidateAttemptNumber(attemptNumber);
        ValidateStartedAt(startedAt);
        ValidateFinishedAt(startedAt, finishedAt);
        ValidateOutcome(outcome);
        ValidateProviderMessageId(providerMessageId);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorClass(errorClass);
        ValidateErrorDetail(errorDetail);
        ValidateCorrelationId(correlationId);

        return new EmailDeliveryAttempt
        {
            EmailDeliveryId = emailDeliveryId,
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
            CreatedAt = DateTime.UtcNow
        };
    }

    public static EmailDeliveryAttempt Rehydrate(
        long emailDeliveryAttemptId,
        long emailDeliveryId,
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
        ValidateAttemptNumber(attemptNumber);
        ValidateStartedAt(startedAt);
        ValidateFinishedAt(startedAt, finishedAt);
        ValidateOutcome(outcome);
        ValidateProviderMessageId(providerMessageId);
        ValidateProviderErrorCode(providerErrorCode);
        ValidateErrorClass(errorClass);
        ValidateErrorDetail(errorDetail);
        ValidateCorrelationId(correlationId);

        return new EmailDeliveryAttempt
        {
            EmailDeliveryAttemptId = emailDeliveryAttemptId,
            EmailDeliveryId = emailDeliveryId,
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

    private static void ValidateEmailDeliveryId(long emailDeliveryId)
    {
        if (emailDeliveryId <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_EMAIL_DELIVERY_ID",
                "Email delivery id must be greater than zero.");
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