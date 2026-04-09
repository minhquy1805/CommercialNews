using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Notifications.Application.Ports.Persistence.Write;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence.Exceptions;
using Notifications.Infrastructure.Persistence.Sql;

namespace Notifications.Infrastructure.Persistence.Repositories.Write;

public sealed class EmailDeliveryAttemptRepository : IEmailDeliveryAttemptRepository
{
    private const string EmailDeliveryAttemptInsertProc =
        "[notifications].[EmailDeliveryAttempt_Insert]";

    private const string EmailDeliveryAttemptSelectByEmailDeliveryIdProc =
        "[notifications].[EmailDeliveryAttempt_SelectByEmailDeliveryId]";

    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly NotificationsSqlExceptionTranslator _sqlExceptionTranslator;

    public EmailDeliveryAttemptRepository(
        NotificationsUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        NotificationsSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<long> InsertAsync(
        EmailDeliveryAttempt emailDeliveryAttempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailDeliveryAttempt);

        try
        {
            using SqlCommand command = CreateTransactionalCommand(EmailDeliveryAttemptInsertProc);

            SqlParameter emailDeliveryAttemptIdParameter = new("@EmailDeliveryAttemptId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.AddRange(
            [
                new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryAttempt.EmailDeliveryId },
                new SqlParameter("@AttemptNumber", SqlDbType.Int) { Value = emailDeliveryAttempt.AttemptNumber },
                new SqlParameter("@StartedAt", SqlDbType.DateTime2) { Value = emailDeliveryAttempt.StartedAt },
                new SqlParameter("@FinishedAt", SqlDbType.DateTime2) { Value = ToDbValue(emailDeliveryAttempt.FinishedAt) },
                new SqlParameter("@Outcome", SqlDbType.VarChar, 30) { Value = emailDeliveryAttempt.Outcome },
                new SqlParameter("@IsAmbiguous", SqlDbType.Bit) { Value = emailDeliveryAttempt.IsAmbiguous },
                new SqlParameter("@ProviderMessageId", SqlDbType.NVarChar, 200) { Value = ToDbValue(emailDeliveryAttempt.ProviderMessageId) },
                new SqlParameter("@ProviderErrorCode", SqlDbType.NVarChar, 100) { Value = ToDbValue(emailDeliveryAttempt.ProviderErrorCode) },
                new SqlParameter("@ErrorClass", SqlDbType.VarChar, 30) { Value = ToDbValue(emailDeliveryAttempt.ErrorClass) },
                new SqlParameter("@ErrorDetail", SqlDbType.NVarChar, 2000) { Value = ToDbValue(emailDeliveryAttempt.ErrorDetail) },
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = ToDbValue(emailDeliveryAttempt.CorrelationId) },
                emailDeliveryAttemptIdParameter
            ]);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return emailDeliveryAttemptIdParameter.Value is DBNull
                ? 0
                : Convert.ToInt64(emailDeliveryAttemptIdParameter.Value);
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<IReadOnlyList<EmailDeliveryAttempt>> GetByEmailDeliveryIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            (SqlCommand command, SqlConnection? connection) =
                await CreateReadCommandAsync(EmailDeliveryAttemptSelectByEmailDeliveryIdProc, cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@EmailDeliveryId", SqlDbType.BigInt) { Value = emailDeliveryId });

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                List<EmailDeliveryAttempt> items = [];

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapEmailDeliveryAttempt(reader));
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

    private static EmailDeliveryAttempt MapEmailDeliveryAttempt(SqlDataReader reader)
    {
        return EmailDeliveryAttempt.Rehydrate(
            emailDeliveryAttemptId: reader.GetInt64(reader.GetOrdinal("EmailDeliveryAttemptId")),
            emailDeliveryId: reader.GetInt64(reader.GetOrdinal("EmailDeliveryId")),
            attemptNumber: reader.GetInt32(reader.GetOrdinal("AttemptNumber")),
            startedAt: reader.GetDateTime(reader.GetOrdinal("StartedAt")),
            finishedAt: GetNullableDateTime(reader, "FinishedAt"),
            outcome: reader.GetString(reader.GetOrdinal("Outcome")),
            isAmbiguous: reader.GetBoolean(reader.GetOrdinal("IsAmbiguous")),
            providerMessageId: GetNullableString(reader, "ProviderMessageId"),
            providerErrorCode: GetNullableString(reader, "ProviderErrorCode"),
            errorClass: GetNullableString(reader, "ErrorClass"),
            errorDetail: GetNullableString(reader, "ErrorDetail"),
            correlationId: GetNullableString(reader, "CorrelationId"),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")));
    }

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}