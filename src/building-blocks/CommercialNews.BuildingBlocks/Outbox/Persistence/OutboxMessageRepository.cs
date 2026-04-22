using System.Data;
using CommercialNews.BuildingBlocks.Outbox.Exceptions;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Microsoft.Data.SqlClient;

namespace CommercialNews.BuildingBlocks.Outbox.Persistence;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private const string OutboxMessageInsertProc =
        "[notifications].[OutboxMessage_Insert]";

    private const string OutboxMessageSelectByIdProc =
        "[notifications].[OutboxMessage_SelectById]";

    private const string OutboxMessageSelectByMessageIdProc =
        "[notifications].[OutboxMessage_SelectByMessageId]";

    private const string OutboxMessageClaimPendingProc =
        "[notifications].[OutboxMessage_ClaimPending]";

    private const string OutboxMessageMarkPublishedProc =
        "[notifications].[OutboxMessage_MarkPublished]";

    private const string OutboxMessageMarkFailedProc =
        "[notifications].[OutboxMessage_MarkFailed]";

    private const string OutboxMessageMarkDeadProc =
        "[notifications].[OutboxMessage_MarkDead]";

    private const string OutboxMessageResetToPendingProc =
        "[notifications].[OutboxMessage_ResetToPending]";

    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly OutboxSqlExceptionTranslator _sqlExceptionTranslator;

    public OutboxMessageRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        OutboxSqlExceptionTranslator sqlExceptionTranslator)
    {
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        ISqlUnitOfWork unitOfWork,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(outboxMessage);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(unitOfWork, OutboxMessageInsertProc);

            SqlParameter outboxMessageIdParameter = new("@OutboxMessageId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = outboxMessage.MessageId },
                new SqlParameter("@EventType", SqlDbType.NVarChar, 200) { Value = outboxMessage.EventType },
                new SqlParameter("@AggregateType", SqlDbType.NVarChar, 100) { Value = outboxMessage.AggregateType },
                new SqlParameter("@AggregateId", SqlDbType.NVarChar, 100) { Value = outboxMessage.AggregateId },
                new SqlParameter("@AggregatePublicId", SqlDbType.Char, 26) { Value = ToDbValue(outboxMessage.AggregatePublicId) },
                new SqlParameter("@AggregateVersion", SqlDbType.Int) { Value = ToDbValue(outboxMessage.AggregateVersion) },
                new SqlParameter("@Payload", SqlDbType.NVarChar, -1) { Value = outboxMessage.Payload },
                new SqlParameter("@Headers", SqlDbType.NVarChar, -1) { Value = ToDbValue(outboxMessage.Headers) },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(outboxMessage.CorrelationId) },
                new SqlParameter("@InitiatorUserId", SqlDbType.BigInt) { Value = ToDbValue(outboxMessage.InitiatorUserId) },
                new SqlParameter("@Priority", SqlDbType.TinyInt) { Value = outboxMessage.Priority },
                new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = outboxMessage.OccurredAt },
                outboxMessageIdParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return outboxMessageIdParameter.Value is DBNull
                ? 0
                : Convert.ToInt64(outboxMessageIdParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<OutboxMessage?> GetByIdAsync(
        long outboxMessageId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(OutboxMessageSelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@OutboxMessageId", SqlDbType.BigInt) { Value = outboxMessageId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapOutboxMessage(reader);
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<OutboxMessage?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(OutboxMessageSelectByMessageIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = messageId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapOutboxMessage(reader);
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int topN,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(OutboxMessageClaimPendingProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@TopN", SqlDbType.Int) { Value = topN },
                    new SqlParameter("@Now", SqlDbType.DateTime2) { Value = nowUtc }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<OutboxMessage> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapOutboxMessage(reader));
                }

                return items;
            }
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
        finally
        {
            if (ownedConnection is not null)
            {
                await ownedConnection.DisposeAsync();
            }
        }
    }

    public async Task<int> MarkPublishedAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(unitOfWork, OutboxMessageMarkPublishedProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@OutboxMessageId", SqlDbType.BigInt) { Value = outboxMessageId },
                affectedRowsParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRowsParameter.Value is DBNull
                ? 0
                : Convert.ToInt32(affectedRowsParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<int> MarkFailedAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        DateTime? nextRetryAt,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(unitOfWork, OutboxMessageMarkFailedProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@OutboxMessageId", SqlDbType.BigInt) { Value = outboxMessageId },
                new SqlParameter("@NextRetryAt", SqlDbType.DateTime2) { Value = ToDbValue(nextRetryAt) },
                new SqlParameter("@LastError", SqlDbType.NVarChar, 2000) { Value = ToDbValue(lastError) },
                new SqlParameter("@LastErrorCode", SqlDbType.NVarChar, 100) { Value = ToDbValue(lastErrorCode) },
                new SqlParameter("@LastErrorClass", SqlDbType.VarChar, 30) { Value = ToDbValue(lastErrorClass) },
                affectedRowsParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRowsParameter.Value is DBNull
                ? 0
                : Convert.ToInt32(affectedRowsParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<int> MarkDeadAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        string? lastError,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(unitOfWork, OutboxMessageMarkDeadProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@OutboxMessageId", SqlDbType.BigInt) { Value = outboxMessageId },
                new SqlParameter("@LastError", SqlDbType.NVarChar, 2000) { Value = ToDbValue(lastError) },
                new SqlParameter("@LastErrorCode", SqlDbType.NVarChar, 100) { Value = ToDbValue(lastErrorCode) },
                new SqlParameter("@LastErrorClass", SqlDbType.VarChar, 30) { Value = ToDbValue(lastErrorClass) },
                affectedRowsParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRowsParameter.Value is DBNull
                ? 0
                : Convert.ToInt32(affectedRowsParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<int> ResetToPendingAsync(
        ISqlUnitOfWork unitOfWork,
        long outboxMessageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(unitOfWork, OutboxMessageResetToPendingProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@OutboxMessageId", SqlDbType.BigInt) { Value = outboxMessageId },
                affectedRowsParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRowsParameter.Value is DBNull
                ? 0
                : Convert.ToInt32(affectedRowsParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private static SqlCommand CreateTransactionalCommand(
        ISqlUnitOfWork unitOfWork,
        string storedProcedureName)
    {
      
        if (!unitOfWork.HasActiveConnection || !unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "OutboxMessageRepository requires an active SQL transaction before executing transactional commands.");
        }

        SqlCommand command = unitOfWork.Connection.CreateCommand();
        command.Transaction = unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateReadCommandAsync(
        string storedProcedureName,
        CancellationToken cancellationToken)
    {
        SqlConnection ownedConnection = _sqlConnectionFactory.CreateConnection();
        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = ownedConnection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static OutboxMessage MapOutboxMessage(SqlDataReader reader)
    {
        return OutboxMessage.Rehydrate(
            outboxMessageId: reader.GetInt64(reader.GetOrdinal("OutboxMessageId")),
            messageId: reader.GetString(reader.GetOrdinal("MessageId")),
            eventType: reader.GetString(reader.GetOrdinal("EventType")),
            aggregateType: reader.GetString(reader.GetOrdinal("AggregateType")),
            aggregateId: reader.GetString(reader.GetOrdinal("AggregateId")),
            payload: reader.GetString(reader.GetOrdinal("Payload")),
            priority: reader.GetByte(reader.GetOrdinal("Priority")),
            status: reader.GetString(reader.GetOrdinal("Status")),
            attemptCount: reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            occurredAt: reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            aggregatePublicId: GetNullableString(reader, "AggregatePublicId"),
            aggregateVersion: GetNullableInt32(reader, "AggregateVersion"),
            headers: GetNullableString(reader, "Headers"),
            correlationId: GetNullableString(reader, "CorrelationId"),
            initiatorUserId: GetNullableInt64(reader, "InitiatorUserId"),
            nextRetryAt: GetNullableDateTime(reader, "NextRetryAt"),
            lastAttemptAt: GetNullableDateTime(reader, "LastAttemptAt"),
            publishedAt: GetNullableDateTime(reader, "PublishedAt"),
            lastError: GetNullableString(reader, "LastError"),
            lastErrorCode: GetNullableString(reader, "LastErrorCode"),
            lastErrorClass: GetNullableString(reader, "LastErrorClass"));
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static int? GetNullableInt32(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}