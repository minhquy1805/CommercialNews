using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Exceptions;

public sealed class AuditSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    private static readonly (string Constraint, string Code, string Message)[] UniqueConstraintMappings =
    [
        (
            "UQ_AuditLog_PublicId",
            "AUDIT.INVALID_PUBLIC_ID",
            "Audit log public id already exists."),
        (
            "UQ_AuditLog_MessageId",
            "AUDIT.INGESTION_DUPLICATE",
            "An audit log with the same message id already exists."),
        (
            "UQ_AuditIngestion_PublicId",
            "AUDIT.INVALID_PUBLIC_ID",
            "Audit ingestion public id already exists."),
        (
            "UQ_AuditIngestion_MessageId",
            "AUDIT.INGESTION_DUPLICATE",
            "An audit ingestion record with the same message id already exists.")
    ];

    private static readonly (string Constraint, string Code, string Message)[] CheckConstraintMappings =
    [
        (
            "CK_AuditLog_PublicId_NotBlank",
            "AUDIT.INVALID_PUBLIC_ID",
            "Audit public id is required."),
        (
            "CK_AuditLog_MessageId_NotBlank",
            "AUDIT.INVALID_MESSAGE_ID",
            "Audit message id is required."),
        (
            "CK_AuditLog_EventType_NotBlank",
            "AUDIT.VALIDATION_FAILED",
            "Audit event type is required."),
        (
            "CK_AuditLog_SourceModule_NotBlank",
            "AUDIT.UNSUPPORTED_SOURCE_MODULE",
            "Audit source module is required."),
        (
            "CK_AuditLog_Action_NotBlank",
            "AUDIT.UNSUPPORTED_ACTION",
            "Audit action is required."),
        (
            "CK_AuditLog_ResourceType_NotBlank",
            "AUDIT.VALIDATION_FAILED",
            "Audit resource type is required."),
        (
            "CK_AuditLog_ResourceId_NotBlank",
            "AUDIT.VALIDATION_FAILED",
            "Audit resource id is required."),
        (
            "CK_AuditLog_Summary_NotBlank",
            "AUDIT.VALIDATION_FAILED",
            "Audit summary is required."),
        (
            "CK_AuditLog_EventVersion",
            "AUDIT.VALIDATION_FAILED",
            "Audit event version is invalid."),
        (
            "CK_AuditLog_AggregateVersion",
            "AUDIT.VALIDATION_FAILED",
            "Audit aggregate version is invalid."),
        (
            "CK_AuditLog_SourcePriority",
            "AUDIT.VALIDATION_FAILED",
            "Audit source priority is invalid."),
        (
            "CK_AuditLog_ActorType",
            "AUDIT.VALIDATION_FAILED",
            "Audit actor type is invalid."),
        (
            "CK_AuditLog_Outcome",
            "AUDIT.UNSUPPORTED_OUTCOME",
            "Audit outcome is invalid."),
        (
            "CK_AuditLog_Severity",
            "AUDIT.UNSUPPORTED_SEVERITY",
            "Audit severity is invalid."),
        (
            "CK_AuditLog_RiskLevel",
            "AUDIT.UNSUPPORTED_RISK_LEVEL",
            "Audit risk level is invalid."),
        (
            "CK_AuditLog_MetadataJson_IsJson",
            "AUDIT.VALIDATION_FAILED",
            "Audit metadata json is invalid."),
        (
            "CK_AuditLog_HeadersJson_IsJson",
            "AUDIT.VALIDATION_FAILED",
            "Audit headers json is invalid."),
        (
            "CK_AuditLog_SanitizedPayloadJson_IsJson",
            "AUDIT.VALIDATION_FAILED",
            "Audit sanitized payload json is invalid."),
        (
            "CK_AuditLog_BeforeJson_IsJson",
            "AUDIT.VALIDATION_FAILED",
            "Audit before json is invalid."),
        (
            "CK_AuditLog_AfterJson_IsJson",
            "AUDIT.VALIDATION_FAILED",
            "Audit after json is invalid."),
        (
            "CK_AuditLog_ChangesJson_IsJson",
            "AUDIT.VALIDATION_FAILED",
            "Audit changes json is invalid."),
        (
            "CK_AuditIngestion_PublicId_NotBlank",
            "AUDIT.INVALID_PUBLIC_ID",
            "Audit ingestion public id is required."),
        (
            "CK_AuditIngestion_MessageId_NotBlank",
            "AUDIT.INVALID_MESSAGE_ID",
            "Audit ingestion message id is required."),
        (
            "CK_AuditIngestion_EventType_NotBlank",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion event type is required."),
        (
            "CK_AuditIngestion_ConsumerName_NotBlank",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion consumer name is required."),
        (
            "CK_AuditIngestion_AggregateVersion",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion aggregate version is invalid."),
        (
            "CK_AuditIngestion_SourcePriority",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion source priority is invalid."),
        (
            "CK_AuditIngestion_Status",
            "AUDIT.UNSUPPORTED_STATUS",
            "Audit ingestion status is invalid."),
        (
            "CK_AuditIngestion_AttemptCount",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion attempt count is invalid."),
        (
            "CK_AuditIngestion_LastErrorClass",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion error class is invalid."),
        (
            "CK_AuditIngestion_SourcePublishedAtUtc",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion source published time is invalid."),
        (
            "CK_AuditIngestion_LastAttemptAtUtc",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion last attempt time is invalid."),
        (
            "CK_AuditIngestion_ProcessedAtUtc",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion processed time is invalid."),
        (
            "CK_AuditIngestion_DeadLetteredAtUtc",
            "AUDIT.VALIDATION_FAILED",
            "Audit ingestion dead-lettered time is invalid.")
    ];

    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapConstraintViolation(exception),
            1205 => Audit(
                code: "AUDIT.DEPENDENCY_UNAVAILABLE",
                message: "The audit database operation deadlocked. Please retry.",
                exception),
            -2 => Audit(
                code: "AUDIT.DEPENDENCY_UNAVAILABLE",
                message: "The audit database operation timed out.",
                exception),
            >= 56201 and <= 56204 => Audit(
                code: "AUDIT.DEPENDENCY_UNAVAILABLE",
                message: "Required audit database objects are unavailable.",
                exception),
            >= 56310 and <= 56349 => MapAuditLogProcedureError(exception),
            >= 56350 and <= 56389 => MapAuditIngestionProcedureError(exception),

            _ => Audit(
                code: "AUDIT.DEPENDENCY_UNAVAILABLE",
                message: "An unexpected SQL persistence error occurred.",
                exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        foreach ((string constraint, string code, string mappedMessage) in UniqueConstraintMappings)
        {
            if (message.Contains(constraint, StringComparison.OrdinalIgnoreCase))
            {
                return Audit(code, mappedMessage, exception);
            }
        }

        return Audit(
            code: "AUDIT.VALIDATION_FAILED",
            message: "An audit uniqueness constraint was violated.",
            exception);
    }

    private static Exception MapConstraintViolation(SqlException exception)
    {
        string message = exception.Message;

        foreach ((string constraint, string code, string mappedMessage) in CheckConstraintMappings)
        {
            if (message.Contains(constraint, StringComparison.OrdinalIgnoreCase))
            {
                return Audit(code, mappedMessage, exception);
            }
        }

        return Audit(
            code: "AUDIT.VALIDATION_FAILED",
            message: "An audit foreign key or check constraint was violated.",
            exception);
    }

    private static Exception MapAuditLogProcedureError(SqlException exception)
    {
        return exception.Number switch
        {
            56310 or 56311 => Audit(
                code: "AUDIT.INVALID_PUBLIC_ID",
                message: "Audit public id is invalid.",
                exception),
            56312 or 56313 => Audit(
                code: "AUDIT.INVALID_MESSAGE_ID",
                message: "Audit message id is invalid.",
                exception),
            56315 => Audit(
                code: "AUDIT.UNSUPPORTED_SOURCE_MODULE",
                message: "Audit source module is invalid.",
                exception),
            56316 => Audit(
                code: "AUDIT.UNSUPPORTED_ACTION",
                message: "Audit action is invalid.",
                exception),
            56325 => Audit(
                code: "AUDIT.UNSUPPORTED_OUTCOME",
                message: "Audit outcome is invalid.",
                exception),
            56326 => Audit(
                code: "AUDIT.UNSUPPORTED_SEVERITY",
                message: "Audit severity is invalid.",
                exception),
            56327 => Audit(
                code: "AUDIT.UNSUPPORTED_RISK_LEVEL",
                message: "Audit risk level is invalid.",
                exception),
            56334 => Audit(
                code: "AUDIT.INVALID_CORRELATION_ID",
                message: "Audit correlation id is invalid.",
                exception),
            56336 => Audit(
                code: "AUDIT.INVALID_TIME_RANGE",
                message: "Audit time range is invalid.",
                exception),
            _ => Audit(
                code: "AUDIT.VALIDATION_FAILED",
                message: "Audit log persistence validation failed.",
                exception)
        };
    }

    private static Exception MapAuditIngestionProcedureError(SqlException exception)
    {
        return exception.Number switch
        {
            56350 or 56351 => Audit(
                code: "AUDIT.INVALID_PUBLIC_ID",
                message: "Audit ingestion public id is invalid.",
                exception),
            56352 or 56353 => Audit(
                code: "AUDIT.INVALID_MESSAGE_ID",
                message: "Audit ingestion message id is invalid.",
                exception),
            56360 => Audit(
                code: "AUDIT.VALIDATION_FAILED",
                message: "Audit ingestion error class is invalid.",
                exception),
            56361 => Audit(
                code: "AUDIT.INVALID_TIME_RANGE",
                message: "Audit ingestion time range is invalid.",
                exception),
            56362 => Audit(
                code: "AUDIT.UNSUPPORTED_STATUS",
                message: "Audit ingestion status is invalid.",
                exception),
            _ => Audit(
                code: "AUDIT.VALIDATION_FAILED",
                message: "Audit ingestion persistence validation failed.",
                exception)
        };
    }

    private static AuditPersistenceException Audit(
        string code,
        string message,
        SqlException exception)
    {
        return new AuditPersistenceException(
            code: code,
            message: message,
            innerException: exception);
    }
}
