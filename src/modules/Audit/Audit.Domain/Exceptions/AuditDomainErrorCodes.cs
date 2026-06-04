namespace Audit.Domain.Exceptions;

public static class AuditDomainErrorCodes
{
    // Common identity
    public const string PublicIdRequired = "AUDIT_DOMAIN.PUBLIC_ID_REQUIRED";
    public const string PublicIdInvalidLength = "AUDIT_DOMAIN.PUBLIC_ID_INVALID_LENGTH";

    public const string MessageIdRequired = "AUDIT_DOMAIN.MESSAGE_ID_REQUIRED";
    public const string MessageIdInvalidLength = "AUDIT_DOMAIN.MESSAGE_ID_INVALID_LENGTH";

    // Source event
    public const string EventTypeRequired = "AUDIT_DOMAIN.EVENT_TYPE_REQUIRED";
    public const string EventTypeTooLong = "AUDIT_DOMAIN.EVENT_TYPE_TOO_LONG";

    public const string SourceModuleRequired = "AUDIT_DOMAIN.SOURCE_MODULE_REQUIRED";
    public const string SourceModuleInvalid = "AUDIT_DOMAIN.SOURCE_MODULE_INVALID";
    public const string SourceModuleTooLong = "AUDIT_DOMAIN.SOURCE_MODULE_TOO_LONG";

    public const string EventVersionInvalid = "AUDIT_DOMAIN.EVENT_VERSION_INVALID";

    // Action
    public const string ActionRequired = "AUDIT_DOMAIN.ACTION_REQUIRED";
    public const string ActionTooLong = "AUDIT_DOMAIN.ACTION_TOO_LONG";

    public const string ActionCategoryInvalid = "AUDIT_DOMAIN.ACTION_CATEGORY_INVALID";
    public const string ActionCategoryTooLong = "AUDIT_DOMAIN.ACTION_CATEGORY_TOO_LONG";

    // Aggregate
    public const string AggregateTypeTooLong = "AUDIT_DOMAIN.AGGREGATE_TYPE_TOO_LONG";
    public const string AggregateIdTooLong = "AUDIT_DOMAIN.AGGREGATE_ID_TOO_LONG";
    public const string AggregatePublicIdInvalidLength = "AUDIT_DOMAIN.AGGREGATE_PUBLIC_ID_INVALID_LENGTH";
    public const string AggregateVersionInvalid = "AUDIT_DOMAIN.AGGREGATE_VERSION_INVALID";

    // Resource
    public const string ResourceTypeRequired = "AUDIT_DOMAIN.RESOURCE_TYPE_REQUIRED";
    public const string ResourceTypeTooLong = "AUDIT_DOMAIN.RESOURCE_TYPE_TOO_LONG";

    public const string ResourceIdRequired = "AUDIT_DOMAIN.RESOURCE_ID_REQUIRED";
    public const string ResourceIdTooLong = "AUDIT_DOMAIN.RESOURCE_ID_TOO_LONG";

    public const string ResourceDisplayNameTooLong = "AUDIT_DOMAIN.RESOURCE_DISPLAY_NAME_TOO_LONG";

    // Actor
    public const string ActorTypeRequired = "AUDIT_DOMAIN.ACTOR_TYPE_REQUIRED";
    public const string ActorTypeInvalid = "AUDIT_DOMAIN.ACTOR_TYPE_INVALID";

    public const string ActorUserIdInvalidLength = "AUDIT_DOMAIN.ACTOR_USER_ID_INVALID_LENGTH";
    public const string ActorEmailTooLong = "AUDIT_DOMAIN.ACTOR_EMAIL_TOO_LONG";
    public const string ActorDisplayNameTooLong = "AUDIT_DOMAIN.ACTOR_DISPLAY_NAME_TOO_LONG";

    // Risk / outcome
    public const string OutcomeRequired = "AUDIT_DOMAIN.OUTCOME_REQUIRED";
    public const string OutcomeInvalid = "AUDIT_DOMAIN.OUTCOME_INVALID";

    public const string SeverityRequired = "AUDIT_DOMAIN.SEVERITY_REQUIRED";
    public const string SeverityInvalid = "AUDIT_DOMAIN.SEVERITY_INVALID";

    public const string RiskLevelRequired = "AUDIT_DOMAIN.RISK_LEVEL_REQUIRED";
    public const string RiskLevelInvalid = "AUDIT_DOMAIN.RISK_LEVEL_INVALID";

    // Summary
    public const string SummaryRequired = "AUDIT_DOMAIN.SUMMARY_REQUIRED";
    public const string SummaryTooLong = "AUDIT_DOMAIN.SUMMARY_TOO_LONG";
    public const string ReasonTooLong = "AUDIT_DOMAIN.REASON_TOO_LONG";

    // Trace / request context
    public const string CorrelationIdTooLong = "AUDIT_DOMAIN.CORRELATION_ID_TOO_LONG";
    public const string CausationIdTooLong = "AUDIT_DOMAIN.CAUSATION_ID_TOO_LONG";
    public const string TraceIdTooLong = "AUDIT_DOMAIN.TRACE_ID_TOO_LONG";

    public const string IpAddressTooLong = "AUDIT_DOMAIN.IP_ADDRESS_TOO_LONG";
    public const string UserAgentTooLong = "AUDIT_DOMAIN.USER_AGENT_TOO_LONG";

    // Priority / time
    public const string SourcePriorityInvalid = "AUDIT_DOMAIN.SOURCE_PRIORITY_INVALID";
    public const string OccurredAtUtcRequired = "AUDIT_DOMAIN.OCCURRED_AT_UTC_REQUIRED";
    public const string SourceOccurredAtUtcRequired = "AUDIT_DOMAIN.SOURCE_OCCURRED_AT_UTC_REQUIRED";
    public const string SourcePublishedAtUtcInvalid = "AUDIT_DOMAIN.SOURCE_PUBLISHED_AT_UTC_INVALID";
    public const string IngestedAtUtcRequired = "AUDIT_DOMAIN.INGESTED_AT_UTC_REQUIRED";
    public const string FirstReceivedAtUtcRequired = "AUDIT_DOMAIN.FIRST_RECEIVED_AT_UTC_REQUIRED";
    public const string TimestampRequired = "AUDIT_DOMAIN.TIMESTAMP_REQUIRED";
    public const string TimestampMustBeUtc = "AUDIT_DOMAIN.TIMESTAMP_MUST_BE_UTC";

    // JSON payload
    public const string JsonPayloadInvalid = "AUDIT_DOMAIN.JSON_PAYLOAD_INVALID";

    // Audit ingestion
    public const string ConsumerNameRequired = "AUDIT_DOMAIN.CONSUMER_NAME_REQUIRED";
    public const string ConsumerNameTooLong = "AUDIT_DOMAIN.CONSUMER_NAME_TOO_LONG";

    public const string IngestionStatusRequired = "AUDIT_DOMAIN.INGESTION_STATUS_REQUIRED";
    public const string IngestionStatusInvalid = "AUDIT_DOMAIN.INGESTION_STATUS_INVALID";
    public const string IngestionStatusTransitionInvalid = "AUDIT_DOMAIN.INGESTION_STATUS_TRANSITION_INVALID";

    public const string AttemptCountInvalid = "AUDIT_DOMAIN.ATTEMPT_COUNT_INVALID";

    // Error info
    public const string ErrorCodeTooLong = "AUDIT_DOMAIN.ERROR_CODE_TOO_LONG";
    public const string ErrorMessageTooLong = "AUDIT_DOMAIN.ERROR_MESSAGE_TOO_LONG";
    public const string ErrorClassInvalid = "AUDIT_DOMAIN.ERROR_CLASS_INVALID";

    // Hash / future tamper-evident
    public const string HashInvalidLength = "AUDIT_DOMAIN.HASH_INVALID_LENGTH";
    public const string PreviousHashInvalidLength = "AUDIT_DOMAIN.PREVIOUS_HASH_INVALID_LENGTH";
}
