using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.BuildingBlocks.Outbox.Errors;

public static class OutboxErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "OUTBOX.VALIDATION_FAILED",
            message: "One or more outbox validations failed.");

    public static readonly Error InvalidRequest =
        Error.Validation(
            code: "OUTBOX.INVALID_REQUEST",
            message: "The outbox request is invalid.");

    public static readonly Error InvalidMessageId =
        Error.Validation(
            code: "OUTBOX.INVALID_MESSAGE_ID",
            message: "The outbox message id format is invalid.");

    public static readonly Error DependencyUnavailable =
        Error.Failure(
            code: "OUTBOX.DEPENDENCY_UNAVAILABLE",
            message: "A required outbox dependency is temporarily unavailable.");

    public static class Message
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "OUTBOX.MESSAGE_NOT_FOUND",
                message: "The outbox message was not found.");

        public static readonly Error InvalidState =
            Error.Validation(
                code: "OUTBOX.MESSAGE_INVALID_STATE",
                message: "The outbox message is not in a valid state for this operation.");

        public static readonly Error UnsupportedEventType =
            Error.Validation(
                code: "OUTBOX.MESSAGE_UNSUPPORTED_EVENT_TYPE",
                message: "The outbox message event type is not supported by the current processor.");

        public static readonly Error PayloadInvalid =
            Error.Validation(
                code: "OUTBOX.MESSAGE_PAYLOAD_INVALID",
                message: "The outbox message payload is invalid.");

        public static readonly Error StaleWriteConflict =
            Error.Conflict(
                code: "OUTBOX.MESSAGE_STALE_WRITE_CONFLICT",
                message: "The outbox message was changed by another process. Please reload and try again.");
    }
}