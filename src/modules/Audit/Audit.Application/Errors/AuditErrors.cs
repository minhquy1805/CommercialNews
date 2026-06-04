using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Audit.Application.Errors;

public static class AuditErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "AUDIT.VALIDATION_FAILED",
            message: "One or more audit validations failed.");

    public static readonly Error PolicyDenied =
        Error.Forbidden(
            code: "AUDIT.POLICY_DENIED",
            message: "You do not have permission to access audit resources.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "AUDIT.RATE_LIMITED",
            message: "Too many audit requests. Please try again later.");

    public static readonly Error DependencyUnavailable =
        Error.Failure(
            code: "AUDIT.DEPENDENCY_UNAVAILABLE",
            message: "A required audit dependency is currently unavailable.");

    public static class Validation
    {
        public static readonly Error InvalidTimeRange =
            Error.Validation(
                code: "AUDIT.INVALID_TIME_RANGE",
                message: "The audit query time range is invalid.");

        public static readonly Error TimeRangeTooLarge =
            Error.Validation(
                code: "AUDIT.TIME_RANGE_TOO_LARGE",
                message: "The audit query time range exceeds the allowed limit.");

        public static readonly Error InvalidPage =
            Error.Validation(
                code: "AUDIT.INVALID_PAGE",
                message: "Page must be greater than zero.");

        public static readonly Error InvalidPageSize =
            Error.Validation(
                code: "AUDIT.INVALID_PAGE_SIZE",
                message: "Page size must be greater than zero.");

        public static readonly Error PageSizeTooLarge =
            Error.Validation(
                code: "AUDIT.PAGE_SIZE_TOO_LARGE",
                message: "Page size exceeds the maximum allowed value.");

        public static readonly Error InvalidSort =
            Error.Validation(
                code: "AUDIT.INVALID_SORT",
                message: "The provided sort field or direction is not supported.");

        public static readonly Error InvalidFilter =
            Error.Validation(
                code: "AUDIT.INVALID_FILTER",
                message: "One or more audit filters are invalid.");

        public static readonly Error InvalidPublicId =
            Error.Validation(
                code: "AUDIT.INVALID_PUBLIC_ID",
                message: "The audit public id is invalid.");

        public static readonly Error InvalidMessageId =
            Error.Validation(
                code: "AUDIT.INVALID_MESSAGE_ID",
                message: "The audit message id is invalid.");

        public static readonly Error InvalidCorrelationId =
            Error.Validation(
                code: "AUDIT.INVALID_CORRELATION_ID",
                message: "The audit correlation id is invalid.");

        public static readonly Error UnsupportedSourceModule =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_SOURCE_MODULE",
                message: "The audit source module is not supported.");

        public static readonly Error UnsupportedAction =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_ACTION",
                message: "The audit action is not supported.");

        public static readonly Error UnsupportedStatus =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_STATUS",
                message: "The audit ingestion status is not supported.");

        public static readonly Error UnsupportedRiskLevel =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_RISK_LEVEL",
                message: "The audit risk level is not supported.");

        public static readonly Error UnsupportedSeverity =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_SEVERITY",
                message: "The audit severity is not supported.");

        public static readonly Error UnsupportedOutcome =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_OUTCOME",
                message: "The audit outcome is not supported.");
    }

    public static class AuditLog
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "AUDIT.LOG_NOT_FOUND",
                message: "Audit log was not found.");

        public static readonly Error MessageNotFound =
            Error.NotFound(
                code: "AUDIT.MESSAGE_NOT_FOUND",
                message: "No audit log was found for the provided message id.");

        public static readonly Error CorrelationNotFound =
            Error.NotFound(
                code: "AUDIT.CORRELATION_NOT_FOUND",
                message: "No audit logs were found for the provided correlation id.");

        public static readonly Error ReadFailed =
            Error.Failure(
                code: "AUDIT.READ_FAILED",
                message: "Audit log read failed.");
    }

    public static class Ingestion
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "AUDIT.INGESTION_NOT_FOUND",
                message: "Audit ingestion record was not found.");

        public static readonly Error MessageNotFound =
            Error.NotFound(
                code: "AUDIT.MESSAGE_NOT_FOUND",
                message: "No audit ingestion record was found for the provided message id.");

        public static readonly Error QueryFailed =
            Error.Failure(
                code: "AUDIT.INGESTION_QUERY_FAILED",
                message: "Audit ingestion query failed.");

        public static readonly Error Failed =
            Error.Failure(
                code: "AUDIT.INGESTION_FAILED",
                message: "Audit consumer processing failed.");

        public static readonly Error DeadLettered =
            Error.Failure(
                code: "AUDIT.INGESTION_DEADLETTERED",
                message: "Audit message reached terminal dead-letter handling.");

        public static readonly Error Duplicate =
            Error.Failure(
                code: "AUDIT.INGESTION_DUPLICATE",
                message: "Audit message was processed as duplicate-safe.");

        public static readonly Error Ignored =
            Error.Failure(
                code: "AUDIT.INGESTION_IGNORED",
                message: "Audit message was intentionally ignored by policy.");

        public static readonly Error Processing =
            Error.Failure(
                code: "AUDIT.INGESTION_PROCESSING",
                message: "Audit message is still processing.");

        public static readonly Error StatusUnknown =
            Error.Failure(
                code: "AUDIT.INGESTION_STATUS_UNKNOWN",
                message: "Audit ingestion status cannot be determined.");

        public static readonly Error MessageSeenButLogMissing =
            Error.Failure(
                code: "AUDIT.MESSAGE_SEEN_BUT_LOG_MISSING",
                message: "Audit has seen the message but no audit log exists yet.");

        public static readonly Error MessagePublishedButNotIngested =
            Error.Failure(
                code: "AUDIT.MESSAGE_PUBLISHED_BUT_NOT_INGESTED",
                message: "The message appears to be published but has not been ingested by Audit.");
    }

    public static class Dashboard
    {
        public static readonly Error QueryFailed =
            Error.Failure(
                code: "AUDIT.DASHBOARD_QUERY_FAILED",
                message: "Audit dashboard query failed.");
    }

    public static class Redaction
    {
        public static readonly Error Violation =
            Error.Failure(
                code: "AUDIT.REDACTION_VIOLATION",
                message: "The audit payload violates redaction rules.");

        public static readonly Error RawPayloadAccessDenied =
            Error.Forbidden(
                code: "AUDIT.RAW_PAYLOAD_ACCESS_DENIED",
                message: "You do not have permission to access sensitive audit payload details.");

        public static readonly Error SensitiveFieldBlocked =
            Error.Forbidden(
                code: "AUDIT.SENSITIVE_FIELD_BLOCKED",
                message: "The requested audit field is blocked by privacy policy.");

        public static readonly Error PayloadNotAvailable =
            Error.NotFound(
                code: "AUDIT.PAYLOAD_NOT_AVAILABLE",
                message: "Audit payload is not available for this record.");

        public static readonly Error PayloadRedacted =
            Error.Failure(
                code: "AUDIT.PAYLOAD_REDACTED",
                message: "Audit payload exists but has been redacted by policy.");
    }
}
