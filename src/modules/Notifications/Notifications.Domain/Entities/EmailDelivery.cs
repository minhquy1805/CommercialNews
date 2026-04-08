using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Domain.Entities;

public sealed class EmailDelivery
{
    public long EmailDeliveryId { get; private set; }

    public string MessageId { get; private set; }

    public string BusinessDedupeKey { get; private set; }

    public long? RecipientUserId { get; private set; }

    public string ToEmail { get; private set; }

    public string? ToEmailHash { get; private set; }

    public string TemplateKey { get; private set; }

    public int? TemplateVersion { get; private set; }

    public string? Subject { get; private set; }

    public string Provider { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public string Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? LastAttemptAt { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    public DateTime? DeadAt { get; private set; }

    public DateTime? SuppressedAt { get; private set; }

    public DateTime? AmbiguousAt { get; private set; }

    public string? LastError { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorClass { get; private set; }

    public string? CorrelationId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private EmailDelivery()
    {
    }

    public static EmailDelivery Create(
        string messageId,
        string businessDedupeKey,
        string toEmail,
        string templateKey,
        string provider,
        DateTime nowUtc,
        long? recipientUserId = null,
        string? toEmailHash = null,
        int? templateVersion = null,
        string? subject = null,
        string? correlationId = null)
    {
        ValidateMessageId(messageId);
        ValidateBusinessDedupeKey(businessDedupeKey);
        ValidateRecipientUserId(recipientUserId);
        ValidateToEmail(toEmail);
        ValidateToEmailHash(toEmailHash);
        ValidateTemplateKey(templateKey);
        ValidateTemplateVersion(templateVersion);
        ValidateSubject(subject);
        ValidateProvider(provider);
        ValidateCorrelationId(correlationId);
        ValidateNowUtc(nowUtc);

        return new EmailDelivery
        {
            MessageId = NormalizeRequired(messageId),
            BusinessDedupeKey = NormalizeRequired(businessDedupeKey),
            RecipientUserId = recipientUserId,
            ToEmail = NormalizeRequired(toEmail),
            ToEmailHash = NormalizeOptional(toEmailHash),
            TemplateKey = NormalizeRequired(templateKey),
            TemplateVersion = templateVersion,
            Subject = NormalizeOptional(subject),
            Provider = NormalizeRequired(provider),
            Status = EmailDeliveryStatus.Queued,
            AttemptCount = 0,
            CorrelationId = NormalizeOptional(correlationId),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public static EmailDelivery Rehydrate(
        long emailDeliveryId,
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string? toEmailHash,
        string templateKey,
        int? templateVersion,
        string? subject,
        string provider,
        string? providerMessageId,
        string status,
        int attemptCount,
        DateTime? lastAttemptAt,
        DateTime? nextRetryAt,
        DateTime? sentAt,
        DateTime? failedAt,
        DateTime? deadAt,
        DateTime? suppressedAt,
        DateTime? ambiguousAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        string? correlationId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (emailDeliveryId <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_ID",
                "Email delivery id must be greater than zero.");
        }

        ValidateMessageId(messageId);
        ValidateBusinessDedupeKey(businessDedupeKey);
        ValidateRecipientUserId(recipientUserId);
        ValidateToEmail(toEmail);
        ValidateToEmailHash(toEmailHash);
        ValidateTemplateKey(templateKey);
        ValidateTemplateVersion(templateVersion);
        ValidateSubject(subject);
        ValidateProvider(provider);
        ValidateStatus(status);
        ValidateAttemptCount(attemptCount);
        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);
        ValidateCorrelationId(correlationId);

        return new EmailDelivery
        {
            EmailDeliveryId = emailDeliveryId,
            MessageId = NormalizeRequired(messageId),
            BusinessDedupeKey = NormalizeRequired(businessDedupeKey),
            RecipientUserId = recipientUserId,
            ToEmail = NormalizeRequired(toEmail),
            ToEmailHash = NormalizeOptional(toEmailHash),
            TemplateKey = NormalizeRequired(templateKey),
            TemplateVersion = templateVersion,
            Subject = NormalizeOptional(subject),
            Provider = NormalizeRequired(provider),
            ProviderMessageId = NormalizeOptional(providerMessageId),
            Status = NormalizeRequired(status),
            AttemptCount = attemptCount,
            LastAttemptAt = lastAttemptAt,
            NextRetryAt = nextRetryAt,
            SentAt = sentAt,
            FailedAt = failedAt,
            DeadAt = deadAt,
            SuppressedAt = suppressedAt,
            AmbiguousAt = ambiguousAt,
            LastError = NormalizeOptional(lastError),
            LastErrorCode = NormalizeOptional(lastErrorCode),
            LastErrorClass = NormalizeOptional(lastErrorClass),
            CorrelationId = NormalizeOptional(correlationId),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void MarkSending(DateTime nowUtc)
    {
        ValidateNowUtc(nowUtc);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Ambiguous);

        Status = EmailDeliveryStatus.Sending;
        LastAttemptAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public void MarkSent(DateTime nowUtc, string? providerMessageId = null)
    {
        ValidateNowUtc(nowUtc);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Sending,
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Ambiguous);

        if (!string.IsNullOrWhiteSpace(providerMessageId) && providerMessageId.Trim().Length > 200)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_PROVIDER_MESSAGE_ID_TOO_LONG",
                "Provider message id must not exceed 200 characters.");
        }

        Status = EmailDeliveryStatus.Sent;
        AttemptCount++;
        ProviderMessageId = NormalizeOptional(providerMessageId);
        SentAt = nowUtc;
        FailedAt = null;
        DeadAt = null;
        SuppressedAt = null;
        AmbiguousAt = null;
        NextRetryAt = null;
        LastError = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    public void MarkFailed(
        DateTime nowUtc,
        DateTime? nextRetryAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass)
    {
        ValidateNowUtc(nowUtc);
        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Sending,
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Ambiguous);

        Status = EmailDeliveryStatus.Failed;
        AttemptCount++;
        FailedAt = nowUtc;
        NextRetryAt = nextRetryAt;
        LastError = NormalizeOptional(lastError);
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void MarkDead(
        DateTime nowUtc,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass)
    {
        ValidateNowUtc(nowUtc);
        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Sending,
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Ambiguous);

        Status = EmailDeliveryStatus.Dead;
        AttemptCount++;
        DeadAt = nowUtc;
        NextRetryAt = null;
        LastError = NormalizeOptional(lastError);
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void MarkSuppressed(
        DateTime nowUtc,
        string? lastError = null,
        string? lastErrorCode = null,
        string? lastErrorClass = EmailErrorClass.Policy)
    {
        ValidateNowUtc(nowUtc);
        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Sending,
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Ambiguous);

        Status = EmailDeliveryStatus.Suppressed;
        SuppressedAt = nowUtc;
        NextRetryAt = null;
        LastError = NormalizeOptional(lastError);
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void MarkAmbiguous(
        DateTime nowUtc,
        DateTime? nextRetryAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass = EmailErrorClass.Ambiguous)
    {
        ValidateNowUtc(nowUtc);
        ValidateErrorCode(lastErrorCode);
        ValidateErrorClass(lastErrorClass);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Sending,
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Ambiguous);

        Status = EmailDeliveryStatus.Ambiguous;
        AttemptCount++;
        AmbiguousAt = nowUtc;
        NextRetryAt = nextRetryAt;
        LastError = NormalizeOptional(lastError);
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void ResetToQueued(DateTime nowUtc)
    {
        ValidateNowUtc(nowUtc);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Failed,
            EmailDeliveryStatus.Dead,
            EmailDeliveryStatus.Ambiguous,
            EmailDeliveryStatus.Suppressed,
            EmailDeliveryStatus.Sending);

        Status = EmailDeliveryStatus.Queued;
        NextRetryAt = null;
        LastError = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    private void EnsureCanTransitionFrom(params string[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE_TRANSITION",
                $"Cannot transition email delivery from status '{Status}'.");
        }
    }

    private static void ValidateMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_MESSAGE_ID_REQUIRED",
                "Message id is required.");
        }

        if (messageId.Trim().Length > 26)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_MESSAGE_ID_TOO_LONG",
                "Message id must not exceed 26 characters.");
        }
    }

    private static void ValidateBusinessDedupeKey(string? businessDedupeKey)
    {
        if (string.IsNullOrWhiteSpace(businessDedupeKey))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_BUSINESS_DEDUPE_KEY_REQUIRED",
                "Business dedupe key is required.");
        }

        if (businessDedupeKey.Trim().Length > 300)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_BUSINESS_DEDUPE_KEY_TOO_LONG",
                "Business dedupe key must not exceed 300 characters.");
        }
    }

    private static void ValidateRecipientUserId(long? recipientUserId)
    {
        if (recipientUserId is not null && recipientUserId <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_RECIPIENT_USER_ID_INVALID",
                "Recipient user id must be greater than zero.");
        }
    }

    private static void ValidateToEmail(string? toEmail)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_TO_EMAIL_REQUIRED",
                "Recipient email is required.");
        }

        if (toEmail.Trim().Length > 320)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_TO_EMAIL_TOO_LONG",
                "Recipient email must not exceed 320 characters.");
        }
    }

    private static void ValidateToEmailHash(string? toEmailHash)
    {
        if (!string.IsNullOrWhiteSpace(toEmailHash) && toEmailHash.Trim().Length > 64)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_TO_EMAIL_HASH_TOO_LONG",
                "Recipient email hash must not exceed 64 characters.");
        }
    }

    private static void ValidateTemplateKey(string? templateKey)
    {
        if (!NotificationTemplateKey.IsValid(templateKey))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_KEY_INVALID",
                "Template key is invalid.");
        }
    }

    private static void ValidateTemplateVersion(int? templateVersion)
    {
        if (templateVersion is not null && templateVersion <= 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_VERSION_INVALID",
                "Template version must be greater than zero.");
        }
    }

    private static void ValidateSubject(string? subject)
    {
        if (!string.IsNullOrWhiteSpace(subject) && subject.Trim().Length > 300)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_SUBJECT_TOO_LONG",
                "Subject must not exceed 300 characters.");
        }
    }

    private static void ValidateProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_PROVIDER_REQUIRED",
                "Provider is required.");
        }

        if (provider.Trim().Length > 30)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_PROVIDER_TOO_LONG",
                "Provider must not exceed 30 characters.");
        }
    }

    private static void ValidateStatus(string? status)
    {
        if (!EmailDeliveryStatus.IsValid(status))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_STATUS_INVALID",
                "Email delivery status is invalid.");
        }
    }

    private static void ValidateAttemptCount(int attemptCount)
    {
        if (attemptCount < 0)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_COUNT_INVALID",
                "Attempt count must not be negative.");
        }
    }

    private static void ValidateErrorCode(string? errorCode)
    {
        if (!string.IsNullOrWhiteSpace(errorCode) && errorCode.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ERROR_CODE_TOO_LONG",
                "Error code must not exceed 100 characters.");
        }
    }

    private static void ValidateErrorClass(string? errorClass)
    {
        if (!string.IsNullOrWhiteSpace(errorClass) && !EmailErrorClass.IsValid(errorClass))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_ERROR_CLASS_INVALID",
                "Error class is invalid.");
        }
    }

    private static void ValidateCorrelationId(string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId) && correlationId.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_CORRELATION_ID_TOO_LONG",
                "Correlation id must not exceed 100 characters.");
        }
    }

    private static void ValidateNowUtc(DateTime nowUtc)
    {
        if (nowUtc == default)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_NOW_UTC_REQUIRED",
                "Current UTC time is required.");
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