using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Audit.Infrastructure.Persistence.Exceptions;

public sealed class AuditSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => new AuditPersistenceException(
                code: "AUDIT.VALIDATION_FAILED",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_AuditLog_AuditEventId", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.DUPLICATE_AUDIT_EVENT",
                message: "The audit event was already recorded.",
                innerException: exception);
        }

        return new AuditPersistenceException(
            code: "AUDIT.VALIDATION_FAILED",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_AuditLog_ActorUser", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_INVALID_ACTOR_USER_ID",
                message: "The referenced actor user does not exist.",
                innerException: exception);
        }

        if (message.Contains("CK_AuditLog_AuditEventId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_INVALID_AUDIT_EVENT_ID",
                message: "Audit event id is required.",
                innerException: exception);
        }

        if (message.Contains("CK_AuditLog_Action_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_ACTION_REQUIRED",
                message: "Audit action is required.",
                innerException: exception);
        }

        if (message.Contains("CK_AuditLog_ResourceType_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_RESOURCE_TYPE_REQUIRED",
                message: "Audit resource type is required.",
                innerException: exception);
        }

        if (message.Contains("CK_AuditLog_ResourceId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_RESOURCE_ID_REQUIRED",
                message: "Audit resource id is required.",
                innerException: exception);
        }

        if (message.Contains("CK_AuditLog_Summary_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_SUMMARY_REQUIRED",
                message: "Audit summary is required.",
                innerException: exception);
        }

        if (message.Contains("CK_AuditLog_Outcome_Allowed", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditPersistenceException(
                code: "AUDIT.LOG_INVALID_OUTCOME",
                message: "Audit outcome is invalid.",
                innerException: exception);
        }

        return new AuditPersistenceException(
            code: "AUDIT.VALIDATION_FAILED",
            message: "A foreign key or check constraint was violated.",
            innerException: exception);
    }
}