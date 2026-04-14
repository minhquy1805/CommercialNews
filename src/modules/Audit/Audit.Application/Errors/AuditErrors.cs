using CommercialNews.BuildingBlocks.Results;

namespace Audit.Application.Errors;

public static class AuditErrors
{
    public static readonly Error ValidationFailed =
        Error.Validation(
            code: "AUDIT.VALIDATION_FAILED",
            message: "One or more audit validations failed.");

    public static readonly Error RateLimited =
        Error.RateLimited(
            code: "AUDIT.RATE_LIMITED",
            message: "Too many audit requests. Please try again later.");

    public static readonly Error PolicyDenied =
        Error.Forbidden(
            code: "AUDIT.POLICY_DENIED",
            message: "You do not have permission to access audit resources.");

    public static readonly Error RedactionViolation =
        Error.Failure(
            code: "AUDIT.REDACTION_VIOLATION",
            message: "The audit payload violates redaction rules.");

    public static class AuditLog
    {
        public static readonly Error NotFound =
            Error.NotFound(
                code: "AUDIT.LOG_NOT_FOUND",
                message: "Audit record was not found.");

        public static readonly Error InvalidAuditId =
            Error.Validation(
                code: "AUDIT.LOG_INVALID_AUDIT_ID",
                message: "Audit id must be greater than zero.");

        public static readonly Error InvalidAuditEventId =
            Error.Validation(
                code: "AUDIT.LOG_INVALID_AUDIT_EVENT_ID",
                message: "Audit event id is invalid.");

        public static readonly Error InvalidActorUserId =
            Error.Validation(
                code: "AUDIT.LOG_INVALID_ACTOR_USER_ID",
                message: "Actor user id must be greater than zero when provided.");

        public static readonly Error ActionRequired =
            Error.Validation(
                code: "AUDIT.LOG_ACTION_REQUIRED",
                message: "Audit action is required.");

        public static readonly Error ActionTooLong =
            Error.Validation(
                code: "AUDIT.LOG_ACTION_TOO_LONG",
                message: "Audit action must not exceed 120 characters.");

        public static readonly Error ResourceTypeRequired =
            Error.Validation(
                code: "AUDIT.LOG_RESOURCE_TYPE_REQUIRED",
                message: "Audit resource type is required.");

        public static readonly Error ResourceTypeTooLong =
            Error.Validation(
                code: "AUDIT.LOG_RESOURCE_TYPE_TOO_LONG",
                message: "Audit resource type must not exceed 60 characters.");

        public static readonly Error ResourceIdRequired =
            Error.Validation(
                code: "AUDIT.LOG_RESOURCE_ID_REQUIRED",
                message: "Audit resource id is required.");

        public static readonly Error ResourceIdTooLong =
            Error.Validation(
                code: "AUDIT.LOG_RESOURCE_ID_TOO_LONG",
                message: "Audit resource id must not exceed 100 characters.");

        public static readonly Error InvalidOutcome =
            Error.Validation(
                code: "AUDIT.LOG_INVALID_OUTCOME",
                message: "Audit outcome is invalid.");

        public static readonly Error SummaryRequired =
            Error.Validation(
                code: "AUDIT.LOG_SUMMARY_REQUIRED",
                message: "Audit summary is required.");

        public static readonly Error SummaryTooLong =
            Error.Validation(
                code: "AUDIT.LOG_SUMMARY_TOO_LONG",
                message: "Audit summary must not exceed 300 characters.");

        public static readonly Error ReasonTooLong =
            Error.Validation(
                code: "AUDIT.LOG_REASON_TOO_LONG",
                message: "Audit reason must not exceed 500 characters.");

        public static readonly Error InvalidOccurredAt =
            Error.Validation(
                code: "AUDIT.LOG_INVALID_OCCURRED_AT",
                message: "OccurredAt is invalid.");

        public static readonly Error CorrelationIdTooLong =
            Error.Validation(
                code: "AUDIT.LOG_CORRELATION_ID_TOO_LONG",
                message: "Correlation id must not exceed 100 characters.");

        public static readonly Error IpAddressTooLong =
            Error.Validation(
                code: "AUDIT.LOG_IP_ADDRESS_TOO_LONG",
                message: "IP address must not exceed 45 characters.");

        public static readonly Error UserAgentTooLong =
            Error.Validation(
                code: "AUDIT.LOG_USER_AGENT_TOO_LONG",
                message: "User agent must not exceed 300 characters.");
    }

    public static class Query
    {
        public static readonly Error InvalidTimeRange =
            Error.Validation(
                code: "AUDIT.INVALID_TIME_RANGE",
                message: "The provided time range is invalid.");

        public static readonly Error InvalidPage =
            Error.Validation(
                code: "AUDIT.INVALID_PAGE",
                message: "Page must be greater than zero.");

        public static readonly Error InvalidPageSize =
            Error.Validation(
                code: "AUDIT.INVALID_PAGE_SIZE",
                message: "Page size must be greater than zero.");

        public static readonly Error InvalidSort =
            Error.Validation(
                code: "AUDIT.INVALID_SORT",
                message: "The provided sort value is not supported.");

        public static readonly Error CorrelationIdRequired =
            Error.Validation(
                code: "AUDIT.CORRELATION_ID_REQUIRED",
                message: "Correlation id is required.");
    }

    public static class Ingestion
    {
        public static readonly Error DuplicateAuditEvent =
            Error.Conflict(
                code: "AUDIT.DUPLICATE_AUDIT_EVENT",
                message: "The audit event was already recorded.");

        public static readonly Error InsertFailed =
            Error.Failure(
                code: "AUDIT.INSERT_FAILED",
                message: "The audit record could not be persisted.");

        public static readonly Error MappingFailed =
            Error.Failure(
                code: "AUDIT.MAPPING_FAILED",
                message: "The audit event could not be mapped to a canonical audit record.");

        public static readonly Error UnsupportedEventType =
            Error.Validation(
                code: "AUDIT.UNSUPPORTED_EVENT_TYPE",
                message: "The audit event type is not supported.");
    }
}