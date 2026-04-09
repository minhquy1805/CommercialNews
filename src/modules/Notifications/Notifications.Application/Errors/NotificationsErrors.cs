using CommercialNews.BuildingBlocks.Results;

namespace Notifications.Application.Errors;

public static class NotificationsErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "NOTIFICATIONS.VALIDATION_FAILED",
            message: "One or more notification validations failed.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "NOTIFICATIONS.RATE_LIMITED",
            message: "Too many notification requests. Please try again later.");

    public static readonly Error PolicyDenied =
        Error.Forbidden(
            code: "NOTIFICATIONS.POLICY_DENIED",
            message: "The notification action is not allowed by policy.");

    public static readonly Error ProviderFailure =
        Error.Failure(
            code: "NOTIFICATIONS.PROVIDER_FAILURE",
            message: "The email provider failed to process the notification request.");

    public static class OutboxMessage
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "NOTIFICATIONS.OUTBOX_MESSAGE_NOT_FOUND",
                message: "Outbox message was not found.");

        public static readonly Error InvalidId =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_INVALID_ID",
                message: "Outbox message id must be greater than zero.");

        public static readonly Error MessageIdRequired =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_MESSAGE_ID_REQUIRED",
                message: "Message id is required.");

        public static readonly Error MessageIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_MESSAGE_ID_TOO_LONG",
                message: "Message id must not exceed 26 characters.");

        public static readonly Error EventTypeRequired =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_EVENT_TYPE_REQUIRED",
                message: "Event type is required.");

        public static readonly Error EventTypeTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_EVENT_TYPE_TOO_LONG",
                message: "Event type must not exceed 200 characters.");

        public static readonly Error AggregateTypeRequired =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_TYPE_REQUIRED",
                message: "Aggregate type is required.");

        public static readonly Error AggregateTypeTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_TYPE_TOO_LONG",
                message: "Aggregate type must not exceed 100 characters.");

        public static readonly Error AggregateIdRequired =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_ID_REQUIRED",
                message: "Aggregate id is required.");

        public static readonly Error AggregateIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_ID_TOO_LONG",
                message: "Aggregate id must not exceed 100 characters.");

        public static readonly Error AggregatePublicIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_PUBLIC_ID_TOO_LONG",
                message: "Aggregate public id must not exceed 26 characters.");

        public static readonly Error AggregateVersionInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_AGGREGATE_VERSION_INVALID",
                message: "Aggregate version must be greater than zero.");

        public static readonly Error PayloadRequired =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_PAYLOAD_REQUIRED",
                message: "Payload is required.");

        public static readonly Error HeadersTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_HEADERS_TOO_LONG",
                message: "Headers must not exceed 4000 characters.");

        public static readonly Error CorrelationIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_CORRELATION_ID_TOO_LONG",
                message: "Correlation id must not exceed 100 characters.");

        public static readonly Error PriorityInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_PRIORITY_INVALID",
                message: "Priority must be between 1 and 9.");

        public static readonly Error StatusInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_STATUS_INVALID",
                message: "Outbox message status is invalid.");

        public static readonly Error AttemptCountInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_ATTEMPT_COUNT_INVALID",
                message: "Attempt count must not be negative.");

        public static readonly Error ErrorCodeTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_ERROR_CODE_TOO_LONG",
                message: "Error code must not exceed 100 characters.");

        public static readonly Error ErrorClassInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_ERROR_CLASS_INVALID",
                message: "Error class is invalid.");

        public static readonly Error OccurredAtRequired =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_OCCURRED_AT_REQUIRED",
                message: "OccurredAt is required.");

        public static readonly Error InvalidStateTransition =
            Error.Conflict(
                code: "NOTIFICATIONS.OUTBOX_INVALID_STATE_TRANSITION",
                message: "The outbox message state transition is not allowed.");

        public static readonly Error AlreadyPublished =
            Error.Conflict(
                code: "NOTIFICATIONS.OUTBOX_ALREADY_PUBLISHED",
                message: "The outbox message has already been published.");

        public static readonly Error DeadLettered =
            Error.Conflict(
                code: "NOTIFICATIONS.OUTBOX_DEAD_LETTERED",
                message: "The outbox message is already dead-lettered and cannot be processed normally.");

        public static readonly Error PayloadInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.OUTBOX_PAYLOAD_INVALID",
                message: "The outbox payload is invalid or missing required fields.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "NOTIFICATIONS.OUTBOX_STALE_WRITE_CONFLICT",
                message: "The outbox message was modified by another operation. Please retry.");
    }

    public static class EmailDelivery
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_NOT_FOUND",
                message: "Email delivery was not found.");

        public static readonly Error InvalidId =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_ID",
                message: "Email delivery id must be greater than zero.");

        public static readonly Error MessageIdRequired =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_MESSAGE_ID_REQUIRED",
                message: "Message id is required.");

        public static readonly Error MessageIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_MESSAGE_ID_TOO_LONG",
                message: "Message id must not exceed 26 characters.");

        public static readonly Error BusinessDedupeKeyRequired =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_BUSINESS_DEDUPE_KEY_REQUIRED",
                message: "Business dedupe key is required.");

        public static readonly Error BusinessDedupeKeyTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_BUSINESS_DEDUPE_KEY_TOO_LONG",
                message: "Business dedupe key must not exceed 300 characters.");

        public static readonly Error RecipientUserIdInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_RECIPIENT_USER_ID_INVALID",
                message: "Recipient user id must be greater than zero.");

        public static readonly Error ToEmailRequired =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_TO_EMAIL_REQUIRED",
                message: "Recipient email is required.");

        public static readonly Error ToEmailTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_TO_EMAIL_TOO_LONG",
                message: "Recipient email must not exceed 320 characters.");

        public static readonly Error ToEmailHashTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_TO_EMAIL_HASH_TOO_LONG",
                message: "Recipient email hash must not exceed 64 characters.");

        public static readonly Error TemplateKeyInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_KEY_INVALID",
                message: "Template key is invalid.");

        public static readonly Error TemplateVersionInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_TEMPLATE_VERSION_INVALID",
                message: "Template version must be greater than zero.");

        public static readonly Error SubjectTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_SUBJECT_TOO_LONG",
                message: "Subject must not exceed 300 characters.");

        public static readonly Error ProviderRequired =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_PROVIDER_REQUIRED",
                message: "Provider is required.");

        public static readonly Error ProviderTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_PROVIDER_TOO_LONG",
                message: "Provider must not exceed 30 characters.");

        public static readonly Error ProviderMessageIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_PROVIDER_MESSAGE_ID_TOO_LONG",
                message: "Provider message id must not exceed 200 characters.");

        public static readonly Error StatusInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_STATUS_INVALID",
                message: "Email delivery status is invalid.");

        public static readonly Error AttemptCountInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_COUNT_INVALID",
                message: "Attempt count must not be negative.");

        public static readonly Error ErrorCodeTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ERROR_CODE_TOO_LONG",
                message: "Error code must not exceed 100 characters.");

        public static readonly Error ErrorClassInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ERROR_CLASS_INVALID",
                message: "Error class is invalid.");

        public static readonly Error CorrelationIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_CORRELATION_ID_TOO_LONG",
                message: "Correlation id must not exceed 100 characters.");

        public static readonly Error NowUtcRequired =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_NOW_UTC_REQUIRED",
                message: "Current UTC time is required.");

        public static readonly Error InvalidStateTransition =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_INVALID_STATE_TRANSITION",
                message: "The email delivery state transition is not allowed.");

        public static readonly Error AlreadySent =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ALREADY_SENT",
                message: "The email delivery has already been sent.");

        public static readonly Error AlreadyDead =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ALREADY_DEAD",
                message: "The email delivery is already dead and cannot be retried normally.");

        public static readonly Error AlreadySuppressed =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ALREADY_SUPPRESSED",
                message: "The email delivery is suppressed and cannot be processed normally.");

        public static readonly Error AmbiguousProviderOutcome =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_AMBIGUOUS_PROVIDER_OUTCOME",
                message: "The provider result is ambiguous and requires safe handling.");

        public static readonly Error DuplicateBusinessIntent =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_DUPLICATE_BUSINESS_INTENT",
                message: "A delivery already exists for the same canonical business intent.");

        public static readonly Error RetryNotAllowed =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_RETRY_NOT_ALLOWED",
                message: "This email delivery is not eligible for retry.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_STALE_WRITE_CONFLICT",
                message: "The email delivery update is stale and cannot be applied.");
    }

    public static class EmailDeliveryAttempt
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_NOT_FOUND",
                message: "Email delivery attempt was not found.");

        public static readonly Error InvalidId =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ID",
                message: "Email delivery attempt id must be greater than zero.");

        public static readonly Error InvalidEmailDeliveryId =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_EMAIL_DELIVERY_ID",
                message: "Email delivery id must be greater than zero.");

        public static readonly Error InvalidAttemptNumber =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_ATTEMPT_NUMBER",
                message: "Attempt number must be greater than zero.");

        public static readonly Error StartedAtRequired =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_STARTED_AT_REQUIRED",
                message: "StartedAt is required.");

        public static readonly Error InvalidFinishedAt =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_INVALID_FINISHED_AT",
                message: "FinishedAt must be greater than or equal to StartedAt.");

        public static readonly Error OutcomeInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_OUTCOME_INVALID",
                message: "Attempt outcome is invalid.");

        public static readonly Error ProviderMessageIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_PROVIDER_MESSAGE_ID_TOO_LONG",
                message: "Provider message id must not exceed 200 characters.");

        public static readonly Error ProviderErrorCodeTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_PROVIDER_ERROR_CODE_TOO_LONG",
                message: "Provider error code must not exceed 100 characters.");

        public static readonly Error ErrorClassInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_CLASS_INVALID",
                message: "Error class is invalid.");

        public static readonly Error ErrorDetailTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_ERROR_DETAIL_TOO_LONG",
                message: "Error detail must not exceed 2000 characters.");

        public static readonly Error CorrelationIdTooLong =
            Error.Validation(
                code: "NOTIFICATIONS.EMAIL_DELIVERY_ATTEMPT_CORRELATION_ID_TOO_LONG",
                message: "Correlation id must not exceed 100 characters.");
    }

    public static class Template
    {
        public static readonly Error TemplateKeyInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.TEMPLATE_KEY_INVALID",
                message: "Notification template key is invalid.");

        public static readonly Error TemplateRenderFailed =
            Error.Validation(
                code: "NOTIFICATIONS.TEMPLATE_RENDER_FAILED",
                message: "Notification template rendering failed.");

        public static readonly Error UnsafeTemplateVariables =
            Error.Validation(
                code: "NOTIFICATIONS.UNSAFE_TEMPLATE_VARIABLES",
                message: "The template contains unsafe or unsupported variables.");
    }

    public static class Provider
    {
        public static readonly Error Timeout =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_TIMEOUT",
                message: "The email provider timed out while processing the request.");

        public static readonly Error Rejected =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_REJECTED",
                message: "The email provider rejected the notification request.");

        public static readonly Error TemporaryUnavailable =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_TEMPORARILY_UNAVAILABLE",
                message: "The email provider is temporarily unavailable.");

        public static readonly Error AmbiguousOutcome =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_AMBIGUOUS_OUTCOME",
                message: "The email provider returned an ambiguous result.");
    }
}