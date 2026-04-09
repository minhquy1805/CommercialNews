using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Notifications.Application.Models.QueryModels;
using Notifications.Application.Ports.Persistence.Read;
using Notifications.Infrastructure.Persistence.Exceptions;
using Notifications.Infrastructure.Persistence.Sql;

namespace Notifications.Infrastructure.Persistence.Repositories.Read;

public sealed class OutboxMessageQueryRepository : IOutboxMessageQueryRepository
{
    private const string OutboxMessageSelectByIdProc =
        "[notifications].[OutboxMessage_SelectById]";

    private const string OutboxMessageSelectByMessageIdProc =
        "[notifications].[OutboxMessage_SelectByMessageId]";

    private const string OutboxMessageSelectByAggregateProc =
        "[notifications].[OutboxMessage_SelectByAggregate]";

    private const string OutboxMessageSelectByCorrelationIdProc =
        "[notifications].[OutboxMessage_SelectByCorrelationId]";

    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly NotificationsSqlExceptionTranslator _sqlExceptionTranslator;

    public OutboxMessageQueryRepository(
        NotificationsUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        NotificationsSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<OutboxMessageResult?> GetByIdAsync(
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

                return MapOutboxMessageResult(reader);
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

    public async Task<OutboxMessageResult?> GetByMessageIdAsync(
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

                return MapOutboxMessageResult(reader);
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

    public async Task<IReadOnlyList<OutboxMessageResult>> GetByAggregateAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(OutboxMessageSelectByAggregateProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@AggregateType", SqlDbType.NVarChar, 100) { Value = aggregateType },
                    new SqlParameter("@AggregateId", SqlDbType.NVarChar, 100) { Value = aggregateId }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<OutboxMessageResult> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapOutboxMessageResult(reader));
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

    public async Task<IReadOnlyList<OutboxMessageResult>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(OutboxMessageSelectByCorrelationIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = correlationId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<OutboxMessageResult> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapOutboxMessageResult(reader));
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

    private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateReadCommandAsync(
        string storedProcedureName,
        CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveConnection)
        {
            SqlCommand ambientCommand = _unitOfWork.Connection.CreateCommand();
            ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                ? _unitOfWork.Transaction
                : null;
            ambientCommand.CommandText = storedProcedureName;
            ambientCommand.CommandType = CommandType.StoredProcedure;

            return (ambientCommand, null);
        }

        SqlConnection ownedConnection = _sqlConnectionFactory.CreateConnection();
        await ownedConnection.OpenAsync(cancellationToken);

        SqlCommand command = ownedConnection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static OutboxMessageResult MapOutboxMessageResult(SqlDataReader reader)
    {
        return new OutboxMessageResult
        {
            OutboxMessageId = reader.GetInt64(reader.GetOrdinal("OutboxMessageId")),
            MessageId = reader.GetString(reader.GetOrdinal("MessageId")),
            EventType = reader.GetString(reader.GetOrdinal("EventType")),
            AggregateType = reader.GetString(reader.GetOrdinal("AggregateType")),
            AggregateId = reader.GetString(reader.GetOrdinal("AggregateId")),
            AggregatePublicId = GetNullableString(reader, "AggregatePublicId"),
            AggregateVersion = GetNullableInt32(reader, "AggregateVersion"),
            CorrelationId = GetNullableString(reader, "CorrelationId"),
            InitiatorUserId = GetNullableInt64(reader, "InitiatorUserId"),
            Priority = reader.GetByte(reader.GetOrdinal("Priority")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            NextRetryAt = GetNullableDateTime(reader, "NextRetryAt"),
            LastAttemptAt = GetNullableDateTime(reader, "LastAttemptAt"),
            PublishedAt = GetNullableDateTime(reader, "PublishedAt"),
            LastErrorCode = GetNullableString(reader, "LastErrorCode"),
            LastErrorClass = GetNullableString(reader, "LastErrorClass"),
            OccurredAt = reader.GetDateTime(reader.GetOrdinal("OccurredAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

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