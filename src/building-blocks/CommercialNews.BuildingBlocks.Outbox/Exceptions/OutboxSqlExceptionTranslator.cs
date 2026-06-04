using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Outbox.Exceptions;

public sealed class OutboxSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => new OutboxPersistenceException(
                code: $"OUTBOX.SQL_{exception.Number}",
                message: $"SQL {exception.Number}: {exception.Message}",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_OutboxMessage_MessageId", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.MESSAGE_ALREADY_EXISTS",
                message: "An outbox message with the same message id already exists.",
                innerException: exception);
        }

        return new OutboxPersistenceException(
            code: $"OUTBOX.SQL_{exception.Number}",
            message: $"SQL {exception.Number}: {exception.Message}",
            innerException: exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("CK_OutboxMessage_Status", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.STATUS_INVALID",
                message: "Outbox message status is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_Priority", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.PRIORITY_INVALID",
                message: "Outbox message priority is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_AttemptCount", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.ATTEMPT_COUNT_INVALID",
                message: "Outbox message attempt count is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_AggregateVersion", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.AGGREGATE_VERSION_INVALID",
                message: "Outbox message aggregate version is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_LastErrorClass", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.ERROR_CLASS_INVALID",
                message: "Outbox message error class is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_OutboxMessage_OccurredAt", StringComparison.OrdinalIgnoreCase))
        {
            return new OutboxPersistenceException(
                code: "OUTBOX.OCCURRED_AT_INVALID",
                message: "Outbox message occurred time is invalid.",
                innerException: exception);
        }

        return new OutboxPersistenceException(
            code: $"OUTBOX.SQL_{exception.Number}",
            message: $"SQL {exception.Number}: {exception.Message}",
            innerException: exception);
    }
}