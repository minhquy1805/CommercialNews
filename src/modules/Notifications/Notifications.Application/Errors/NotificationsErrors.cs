using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Notifications.Application.Errors;

public static class NotificationsErrors
{
    // ---------------------------------------------------------------------
    // General / request-level / policy
    // ---------------------------------------------------------------------

    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "NOTIFICATIONS.VALIDATION_FAILED",
            message: "One or more notification validations failed.");

    public static readonly Error InvalidQuery =
        Error.Validation(
            code: "NOTIFICATIONS.INVALID_QUERY",
            message: "The notification query is invalid.");

    public static readonly Error InvalidMessageId =
        Error.Validation(
            code: "NOTIFICATIONS.INVALID_MESSAGE_ID",
            message: "The message id format is invalid.");

    public static readonly Error InvalidRequest =
        Error.Validation(
            code: "NOTIFICATIONS.INVALID_REQUEST",
            message: "The notification request is invalid.");

    public static readonly Error Unauthenticated =
        Error.Unauthorized(
            code: "NOTIFICATIONS.UNAUTHENTICATED",
            message: "Authentication is required to perform this action.");

    public static readonly Error PolicyDenied =
        Error.Forbidden(
            code: "NOTIFICATIONS.POLICY_DENIED",
            message: "You do not have permission to perform this action.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "NOTIFICATIONS.RATE_LIMITED",
            message: "Too many notification requests. Please try again later.");

    public static readonly Error OperationNotSupported =
        Error.Conflict(
            code: "NOTIFICATIONS.OPERATION_NOT_SUPPORTED",
            message: "The requested notification operation is not supported.");

    public static readonly Error DependencyUnavailable =
        Error.Failure(
            code: "NOTIFICATIONS.DEPENDENCY_UNAVAILABLE",
            message: "A required dependency is temporarily unavailable.");

    // ---------------------------------------------------------------------
    // Delivery workflow
    // ---------------------------------------------------------------------

    public static class Delivery
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "NOTIFICATIONS.DELIVERY_NOT_FOUND",
                message: "Email delivery was not found.");

        public static readonly Error InvalidState =
            Error.Conflict(
                code: "NOTIFICATIONS.INVALID_DELIVERY_STATE",
                message: "The delivery is in an invalid state for this operation.");

        public static readonly Error NotRetryable =
            Error.Conflict(
                code: "NOTIFICATIONS.DELIVERY_NOT_RETRYABLE",
                message: "The delivery is not eligible for retry in its current state.");

        public static readonly Error AlreadySent =
            Error.Conflict(
                code: "NOTIFICATIONS.DELIVERY_ALREADY_SENT",
                message: "The delivery has already been sent.");

        public static readonly Error RetryNotAllowed =
            Error.Conflict(
                code: "NOTIFICATIONS.RETRY_NOT_ALLOWED",
                message: "Retry is not allowed for this delivery in its current state.");

        public static readonly Error CancelNotAllowed =
            Error.Conflict(
                code: "NOTIFICATIONS.CANCEL_NOT_ALLOWED",
                message: "Cancel is not allowed for this delivery in its current state.");

        public static readonly Error DuplicateBusinessIntent =
            Error.Conflict(
                code: "NOTIFICATIONS.DUPLICATE_BUSINESS_INTENT",
                message: "A delivery already exists for the same canonical business intent.");

        public static readonly Error AmbiguousProviderOutcome =
            Error.Conflict(
                code: "NOTIFICATIONS.PROVIDER_AMBIGUOUS_OUTCOME",
                message: "The provider result is ambiguous and requires safe handling.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "NOTIFICATIONS.STALE_WRITE_CONFLICT",
                message: "The delivery was modified by another operation. Please retry.");
    }

    // ---------------------------------------------------------------------
    // Attempt history / subordinate reads
    // ---------------------------------------------------------------------

    public static class AttemptHistory
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "NOTIFICATIONS.ATTEMPT_HISTORY_NOT_FOUND",
                message: "Email delivery attempt history was not found.");
    }

    // ---------------------------------------------------------------------
    // Suppression (only if capability is implemented)
    // ---------------------------------------------------------------------

    public static class Suppression
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "NOTIFICATIONS.SUPPRESSION_NOT_FOUND",
                message: "Suppression entry was not found.");
    }

    // ---------------------------------------------------------------------
    // Provider / dependency / runtime
    // ---------------------------------------------------------------------

    public static class Provider
    {
        public static readonly Error Failure =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_FAILURE",
                message: "The notification provider could not complete the operation.");

        public static readonly Error Timeout =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_TIMEOUT",
                message: "The notification provider timed out while processing the request.");

        public static readonly Error TemporarilyUnavailable =
            Error.Failure(
                code: "NOTIFICATIONS.DEPENDENCY_UNAVAILABLE",
                message: "A required dependency is temporarily unavailable.");

        public static readonly Error AmbiguousOutcome =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_AMBIGUOUS_OUTCOME",
                message: "The notification provider returned an ambiguous result.");

        public static readonly Error RemediationAcceptanceFailed =
            Error.Failure(
                code: "NOTIFICATIONS.REMEDIATION_ACCEPTANCE_FAILED",
                message: "The remediation request could not be accepted.");
    }

    // ---------------------------------------------------------------------
    // Template rendering / safety
    // ---------------------------------------------------------------------

    public static class Template
    {
        public static readonly Error TemplateKeyInvalid =
            Error.Validation(
                code: "NOTIFICATIONS.INVALID_REQUEST",
                message: "The notification template key is invalid.");

        public static readonly Error RenderFailed =
            Error.Failure(
                code: "NOTIFICATIONS.PROVIDER_FAILURE",
                message: "The notification template could not be rendered safely.");

        public static readonly Error UnsafeVariables =
            Error.Validation(
                code: "NOTIFICATIONS.INVALID_REQUEST",
                message: "The template contains unsafe or unsupported variables.");
    }
}