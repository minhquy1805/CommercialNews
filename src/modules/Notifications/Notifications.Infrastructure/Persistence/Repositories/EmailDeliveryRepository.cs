using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;
using Notifications.Application.Ports.Persistence;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence.Exceptions;
using Notifications.Infrastructure.Persistence.Sql;

namespace Notifications.Infrastructure.Persistence.Repositories;

public sealed class EmailDeliveryRepository : IEmailDeliveryRepository
{
    private const string EmailDeliveryInsertProc =
        "[notifications].[EmailDelivery_Insert]";

    private const string EmailDeliverySelectByIdProc =
        "[notifications].[EmailDelivery_SelectById]";

    private const string EmailDeliverySelectByMessageIdProc =
        "[notifications].[EmailDelivery_SelectByMessageId]";

    private const string EmailDeliverySelectByBusinessDedupeKeyProc =
        "[notifications].[EmailDelivery_SelectByBusinessDedupeKey]";

    private const string EmailDeliveryClaimPendingProc =
        "[notifications].[EmailDelivery_ClaimPending]";

    private const string EmailDeliveryMarkSentProc =
        "[notifications].[EmailDelivery_MarkSent]";

    private const string EmailDeliveryMarkFailedProc =
        "[notifications].[EmailDelivery_MarkFailed]";

    private const string EmailDeliveryMarkDeadProc =
        "[notifications].[EmailDelivery_MarkDead]";

    private const string EmailDeliveryMarkSuppressedProc =
        "[notifications].[EmailDelivery_MarkSuppressed]";

    private const string EmailDeliveryMarkAmbiguousProc =
        "[notifications].[EmailDelivery_MarkAmbiguous]";

    private const string EmailDeliveryResetToQueuedProc =
        "[notifications].[EmailDelivery_ResetToQueued]";

    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly NotificationsSqlExceptionTranslator _sqlExceptionTranslator;

    public EmailDeliveryRepository(
        NotificationsUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        NotificationsSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        EmailDelivery emailDelivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailDelivery);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryInsertProc);

            SqlParameter emailDeliveryIdParameter = new("@EmailDeliveryId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@MessageId", SqlDbType.Char, 26) { Value = emailDelivery.MessageId },
                new SqlParameter("@BusinessDedupeKey", SqlDbType.NVarChar, 300) { Value = emailDelivery.BusinessDedupeKey },
                new SqlParameter("@RecipientUserId", SqlDbType.BigInt) { Value = ToDbValue(emailDelivery.RecipientUserId) },
                new SqlParameter("@ToEmail", SqlDbType.NVarChar, 320) { Value = emailDelivery.ToEmail },
                new SqlParameter("@TemplateKey", SqlDbType.NVarChar, 100) { Value = emailDelivery.TemplateKey },
                new SqlParameter("@Provider", SqlDbType.VarChar, 30) { Value = emailDelivery.Provider },
                new SqlParameter("@Priority", SqlDbType.TinyInt) { Value = emailDelivery.Priority },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(emailDelivery.CorrelationId) },
                emailDeliveryIdParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return emailDeliveryIdParameter.Value is DBNull
                ? 0
                : Convert.ToInt64(emailDeliveryIdParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<EmailDelivery?> GetByIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliverySelectByIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapEmailDelivery(reader);
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

    public async Task<EmailDelivery?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliverySelectByMessageIdProc, cancellationToken);

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

                return MapEmailDelivery(reader);
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

    public async Task<EmailDelivery?> GetByBusinessDedupeKeyAsync(
        string businessDedupeKey,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliverySelectByBusinessDedupeKeyProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@BusinessDedupeKey", SqlDbType.NVarChar, 300) { Value = businessDedupeKey });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapEmailDelivery(reader);
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

    public async Task<IReadOnlyList<EmailDelivery>> ClaimPendingAsync(
        int topN,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliveryClaimPendingProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.AddRange(
                [
                    new SqlParameter("@TopN", SqlDbType.Int) { Value = topN },
                    new SqlParameter("@Now", SqlDbType.DateTime2) { Value = nowUtc }
                ]);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<EmailDelivery> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapEmailDelivery(reader));
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

    public async Task<int> MarkSentAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryMarkSentProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId },
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
        long emailDeliveryId,
        DateTime? nextRetryAt,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryMarkFailedProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId },
                new SqlParameter("@NextRetryAt", SqlDbType.DateTime2) { Value = ToDbValue(nextRetryAt) },
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
        long emailDeliveryId,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryMarkDeadProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId },
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

    public async Task<int> MarkSuppressedAsync(
        long emailDeliveryId,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryMarkSuppressedProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId },
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

    public async Task<int> MarkAmbiguousAsync(
        long emailDeliveryId,
        DateTime? nextRetryAt,
        string? lastErrorCode,
        string? lastErrorClass,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryMarkAmbiguousProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId },
                new SqlParameter("@NextRetryAt", SqlDbType.DateTime2) { Value = ToDbValue(nextRetryAt) },
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

    public async Task<int> ResetToQueuedAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryResetToQueuedProc);

            SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId },
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

    private SqlCommand CreateTransactionalCommand(string storedProcedureName)
    {
        SqlCommand command = _unitOfWork.Connection.CreateCommand();
        command.Transaction = _unitOfWork.Transaction;
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
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

    private static EmailDelivery MapEmailDelivery(SqlDataReader reader)
    {
        return EmailDelivery.Rehydrate(
            emailDeliveryId: reader.GetInt64(reader.GetOrdinal("EmailDeliveryId")),
            messageId: reader.GetString(reader.GetOrdinal("MessageId")),
            businessDedupeKey: reader.GetString(reader.GetOrdinal("BusinessDedupeKey")),
            recipientUserId: GetNullableInt64(reader, "RecipientUserId"),
            toEmail: reader.GetString(reader.GetOrdinal("ToEmail")),
            templateKey: reader.GetString(reader.GetOrdinal("TemplateKey")),
            provider: reader.GetString(reader.GetOrdinal("Provider")),
            priority: reader.GetByte(reader.GetOrdinal("Priority")),
            status: reader.GetString(reader.GetOrdinal("Status")),
            attemptCount: reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            lastAttemptAt: GetNullableDateTime(reader, "LastAttemptAt"),
            nextRetryAt: GetNullableDateTime(reader, "NextRetryAt"),
            sentAt: GetNullableDateTime(reader, "SentAt"),
            lastErrorCode: GetNullableString(reader, "LastErrorCode"),
            lastErrorClass: GetNullableString(reader, "LastErrorClass"),
            correlationId: GetNullableString(reader, "CorrelationId"),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")));
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

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}