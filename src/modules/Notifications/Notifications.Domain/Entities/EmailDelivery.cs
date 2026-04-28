using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;

namespace Notifications.Domain.Entities;

public sealed class EmailDelivery
{
    public long EmailDeliveryId { get; private set; }

    public string MessageId { get; private set; } = null!;

    public string BusinessDedupeKey { get; private set; } = null!;

    public long? RecipientUserId { get; private set; }

    public string ToEmail { get; private set; } = null!;

    public string TemplateKey { get; private set; } = null!;

    public string VariablesJson { get; private set; } = null!;

    public string Provider { get; private set; } = null!;

    public byte Priority { get; private set; }

    public string Status { get; private set; } = null!;

    public int AttemptCount { get; private set; }

    public DateTime? LastAttemptAt { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    public DateTime? SentAt { get; private set; }

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
        string variablesJson,
        string provider,
        byte priority,
        DateTime nowUtc,
        long? recipientUserId = null,
        string? correlationId = null)
    {
        ValidateMessageId(messageId);
        ValidateBusinessDedupeKey(businessDedupeKey);
        ValidateRecipientUserId(recipientUserId);
        ValidateToEmail(toEmail);
        ValidateTemplateKey(templateKey);
        ValidateVariablesJson(variablesJson);
        ValidateProvider(provider);
        ValidatePriority(priority);
        ValidateCorrelationId(correlationId);
        ValidateNowUtc(nowUtc);

        return new EmailDelivery
        {
            MessageId = NormalizeRequired(messageId),
            BusinessDedupeKey = NormalizeRequired(businessDedupeKey),
            RecipientUserId = recipientUserId,
            ToEmail = NormalizeRequired(toEmail),
            TemplateKey = NormalizeRequired(templateKey),
            VariablesJson = NormalizeRequired(variablesJson),
            Provider = NormalizeRequired(provider),
            Priority = priority,
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
        string templateKey,
        string variablesJson,
        string provider,
        byte priority,
        string status,
        int attemptCount,
        DateTime? lastAttemptAt,
        DateTime? nextRetryAt,
        DateTime? sentAt,
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
        ValidateTemplateKey(templateKey);
        ValidateVariablesJson(variablesJson);
        ValidateProvider(provider);
        ValidatePriority(priority);
        ValidateStatus(status);
        ValidateAttemptCount(attemptCount);
        ValidateLastErrorCode(lastErrorCode);
        ValidateLastErrorClass(lastErrorClass);
        ValidateCorrelationId(correlationId);
        ValidateCreatedAt(createdAt);
        ValidateUpdatedAt(createdAt, updatedAt);
        ValidateLastAttemptAt(createdAt, lastAttemptAt);
        ValidateNextRetryAt(createdAt, nextRetryAt);
        ValidateSentAt(createdAt, sentAt);

        return new EmailDelivery
        {
            EmailDeliveryId = emailDeliveryId,
            MessageId = NormalizeRequired(messageId),
            BusinessDedupeKey = NormalizeRequired(businessDedupeKey),
            RecipientUserId = recipientUserId,
            ToEmail = NormalizeRequired(toEmail),
            TemplateKey = NormalizeRequired(templateKey),
            VariablesJson = NormalizeRequired(variablesJson),
            Provider = NormalizeRequired(provider),
            Priority = priority,
            Status = NormalizeRequired(status),
            AttemptCount = attemptCount,
            LastAttemptAt = lastAttemptAt,
            NextRetryAt = nextRetryAt,
            SentAt = sentAt,
            LastErrorCode = NormalizeOptional(lastErrorCode),
            LastErrorClass = NormalizeOptional(lastErrorClass),
            CorrelationId = NormalizeOptional(correlationId),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public bool IsTerminal => EmailDeliveryStatus.IsTerminal(Status);

    public bool IsRetryable => EmailDeliveryStatus.IsRetryable(Status);

    public bool CanBeClaimed(DateTime nowUtc)
    {
        ValidateNowUtc(nowUtc);

        if (Status.Equals(EmailDeliveryStatus.Queued, StringComparison.OrdinalIgnoreCase))
        {
            return NextRetryAt is null || NextRetryAt <= nowUtc;
        }

        if (Status.Equals(EmailDeliveryStatus.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return NextRetryAt is not null && NextRetryAt <= nowUtc;
        }

        return false;
    }

    public void MarkSending(DateTime nowUtc)
    {
        ValidateNowUtc(nowUtc);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Queued,
            EmailDeliveryStatus.Failed);

        Status = EmailDeliveryStatus.Sending;
        AttemptCount++;
        LastAttemptAt = nowUtc;
        NextRetryAt = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    public void MarkSent(DateTime nowUtc)
    {
        ValidateNowUtc(nowUtc);

        EnsureCanTransitionFrom(EmailDeliveryStatus.Sending);

        Status = EmailDeliveryStatus.Sent;
        SentAt = nowUtc;
        NextRetryAt = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    public void MarkFailed(
        DateTime nowUtc,
        DateTime? nextRetryAt,
        string? lastErrorCode,
        string? lastErrorClass)
    {
        ValidateNowUtc(nowUtc);
        ValidateNextRetryAt(nowUtc, nextRetryAt);
        ValidateLastErrorCode(lastErrorCode);
        ValidateLastErrorClass(lastErrorClass);

        EnsureCanTransitionFrom(EmailDeliveryStatus.Sending);

        Status = EmailDeliveryStatus.Failed;
        NextRetryAt = nextRetryAt;
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }

    public void MarkDead(
        DateTime nowUtc,
        string? lastErrorCode,
        string? lastErrorClass)
    {
        ValidateNowUtc(nowUtc);
        ValidateLastErrorCode(lastErrorCode);
        ValidateLastErrorClass(lastErrorClass);

        EnsureCanTransitionFrom(
            EmailDeliveryStatus.Sending,
            EmailDeliveryStatus.Failed);

        Status = EmailDeliveryStatus.Dead;
        NextRetryAt = null;
        LastErrorCode = NormalizeOptional(lastErrorCode);
        LastErrorClass = NormalizeOptional(lastErrorClass);
        UpdatedAt = nowUtc;
    }


    public void RequeueForRetry(DateTime nowUtc)
    {
        ValidateNowUtc(nowUtc);

        EnsureCanTransitionFrom(EmailDeliveryStatus.Failed);

        Status = EmailDeliveryStatus.Queued;
        NextRetryAt = null;
        LastErrorCode = null;
        LastErrorClass = null;
        UpdatedAt = nowUtc;
    }

    public void RaisePriority(byte priority, DateTime nowUtc)
    {
        ValidatePriority(priority);
        ValidateNowUtc(nowUtc);

        if (priority > Priority)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_PRIORITY_ESCALATION_INVALID",
                "Priority escalation must move toward a higher priority level.");
        }

        Priority = priority;
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

    private static void ValidateTemplateKey(string? templateKey)
    {
        if (!NotificationTemplateKey.IsValid(templateKey))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_KEY_INVALID",
                "Template key is invalid.");
        }
    }

    private static void ValidateVariablesJson(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_VARIABLES_JSON_REQUIRED",
                "VariablesJson is required.");
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

    private static void ValidatePriority(byte priority)
    {
        if (priority is < 1 or > 9)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_PRIORITY_INVALID",
                "Priority must be between 1 and 9.");
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

    private static void ValidateLastErrorCode(string? lastErrorCode)
    {
        if (!string.IsNullOrWhiteSpace(lastErrorCode) && lastErrorCode.Trim().Length > 100)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_LAST_ERROR_CODE_TOO_LONG",
                "Last error code must not exceed 100 characters.");
        }
    }

    private static void ValidateLastErrorClass(string? lastErrorClass)
    {
        if (!string.IsNullOrWhiteSpace(lastErrorClass) && !EmailErrorClass.IsValid(lastErrorClass))
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_LAST_ERROR_CLASS_INVALID",
                "Last error class is invalid.");
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

    private static void ValidateCreatedAt(DateTime createdAt)
    {
        if (createdAt == default)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_CREATED_AT_REQUIRED",
                "CreatedAt is required.");
        }
    }

    private static void ValidateUpdatedAt(DateTime createdAt, DateTime updatedAt)
    {
        if (updatedAt == default)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_UPDATED_AT_REQUIRED",
                "UpdatedAt is required.");
        }

        if (updatedAt < createdAt)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_UPDATED_AT_INVALID",
                "UpdatedAt must be greater than or equal to CreatedAt.");
        }
    }

    private static void ValidateLastAttemptAt(DateTime createdAt, DateTime? lastAttemptAt)
    {
        if (lastAttemptAt is not null && lastAttemptAt < createdAt)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_LAST_ATTEMPT_AT_INVALID",
                "LastAttemptAt must be greater than or equal to CreatedAt.");
        }
    }

    private static void ValidateNextRetryAt(DateTime baselineUtc, DateTime? nextRetryAt)
    {
        if (nextRetryAt is not null && nextRetryAt < baselineUtc)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_NEXT_RETRY_AT_INVALID",
                "NextRetryAt must be greater than or equal to the baseline time.");
        }
    }

    private static void ValidateSentAt(DateTime createdAt, DateTime? sentAt)
    {
        if (sentAt is not null && sentAt < createdAt)
        {
            throw new NotificationsDomainException(
                "NOTIFICATIONS.EMAIL_DELIVERY_SENT_AT_INVALID",
                "SentAt must be greater than or equal to CreatedAt.");
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