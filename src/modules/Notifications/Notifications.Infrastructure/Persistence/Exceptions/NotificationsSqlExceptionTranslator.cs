using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Notifications.Infrastructure.Persistence.Exceptions;

public sealed class NotificationsSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => new NotificationsPersistenceException(
                code: "NOTIFICATIONS.VALIDATION_FAILED",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_OutboxMessage_MessageId", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_MESSAGE_ALREADY_EXISTS",
                message: "An outbox message with the same message id already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_EmailDelivery_MessageId", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_MESSAGE_ALREADY_EXISTS",
                message: "An email delivery with the same message id already exists.",
                innerException: exception);
        }

        if (message.Contains("UQ_EmailDelivery_BusinessDedupeKey", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_DUPLICATE_BUSINESS_INTENT",
                message: "An email delivery already exists for the same business dedupe key.",
                innerException: exception);
        }

        if (message.Contains("UQ_EmailDeliveryAttempt_EmailDeliveryId_AttemptNumber", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ALREADY_EXISTS",
                message: "An email delivery attempt already exists for the same delivery and attempt number.",
                innerException: exception);
        }

        return new NotificationsPersistenceException(
            code: "NOTIFICATIONS.VALIDATION_FAILED",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_EmailDelivery_OutboxMessage_MessageId", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_MESSAGE_NOT_FOUND",
                message: "The referenced outbox message does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_EmailDelivery_UserAccount", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.RECIPIENT_USER_NOT_FOUND",
                message: "The referenced recipient user does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_EmailDeliveryAttempt_EmailDelivery", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND",
                message: "The referenced email delivery does not exist.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_Status", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_STATUS_INVALID",
                message: "Outbox message status is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_Priority", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_PRIORITY_INVALID",
                message: "Outbox message priority is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_AttemptCount", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_ATTEMPT_COUNT_INVALID",
                message: "Outbox message attempt count is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_AggregateVersion", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_VERSION_INVALID",
                message: "Outbox message aggregate version is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_LastErrorClass", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_ERROR_CLASS_INVALID",
                message: "Outbox message error class is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_OccurredAt", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.OUTBOX_OCCURRED_AT_INVALID",
                message: "Outbox message occurred time is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDelivery_Status", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_STATUS_INVALID",
                message: "Email delivery status is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDelivery_AttemptCount", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_COUNT_INVALID",
                message: "Email delivery attempt count is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDelivery_TemplateVersion", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_VERSION_INVALID",
                message: "Email delivery template version is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDelivery_LastErrorClass", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ERROR_CLASS_INVALID",
                message: "Email delivery error class is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDelivery_SentAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_EmailDelivery_FailedAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_EmailDelivery_DeadAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_EmailDelivery_SuppressedAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_EmailDelivery_AmbiguousAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_EmailDelivery_LastAttemptAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_EmailDelivery_NextRetryAt", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE",
                message: "Email delivery state is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDeliveryAttempt_AttemptNumber", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ATTEMPT_NUMBER",
                message: "Email delivery attempt number is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDeliveryAttempt_Outcome", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_OUTCOME_INVALID",
                message: "Email delivery attempt outcome is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDeliveryAttempt_ErrorClass", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_CLASS_INVALID",
                message: "Email delivery attempt error class is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_EmailDeliveryAttempt_FinishedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationsPersistenceException(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_FINISHED_AT",
                message: "Email delivery attempt finished time is invalid.",
                innerException: exception);
        }

        return new NotificationsPersistenceException(
            code: "NOTIFICATIONS.VALIDATION_FAILED",
            message: "A foreign key or check constraint was violated.",
            innerException: exception);
    }
}