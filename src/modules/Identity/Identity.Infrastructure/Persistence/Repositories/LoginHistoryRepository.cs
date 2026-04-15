using System.Data;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Identity.Application.Ports.Persistence;
using Identity.Infrastructure.Persistence.Exceptions;
using Identity.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Repositories
{
    public sealed class LoginHistoryRepository : ILoginHistoryRepository
    {
        private const string LoginHistoryInsertProc = "[identity].[LoginHistory_Insert]";

        private readonly IdentityUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly IdentitySqlExceptionTranslator _sqlExceptionTranslator;

        public LoginHistoryRepository(
            IdentityUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            IdentitySqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task InsertAsync(
            long? userId,
            string? emailNormalizedAttempted,
            bool succeeded,
            string? failureReason,
            string? ipAddress,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken = default)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(LoginHistoryInsertProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    AddParameters(
                        command,
                        userId,
                        emailNormalizedAttempted,
                        succeeded,
                        failureReason,
                        ipAddress,
                        userAgent,
                        correlationId);

                    await command.ExecuteNonQueryAsync(cancellationToken);
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

        private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateCommandAsync(
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

        private static void AddParameters(
            SqlCommand command,
            long? userId,
            string? emailNormalizedAttempted,
            bool succeeded,
            string? failureReason,
            string? ipAddress,
            string? userAgent,
            string? correlationId)
        {
            command.Parameters.Add(
                new SqlParameter("@UserId", SqlDbType.BigInt)
                {
                    Value = (object?)userId ?? DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@EmailNormalizedAttempted", SqlDbType.NVarChar, 320)
                {
                    Value = !string.IsNullOrWhiteSpace(emailNormalizedAttempted)
                        ? emailNormalizedAttempted.Trim()
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@Succeeded", SqlDbType.Bit)
                {
                    Value = succeeded
                });

            command.Parameters.Add(
                new SqlParameter("@FailureReason", SqlDbType.NVarChar, 100)
                {
                    Value = !string.IsNullOrWhiteSpace(failureReason)
                        ? failureReason.Trim()
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@IpAddress", SqlDbType.NVarChar, 45)
                {
                    Value = !string.IsNullOrWhiteSpace(ipAddress)
                        ? ipAddress.Trim()
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
                {
                    Value = !string.IsNullOrWhiteSpace(userAgent)
                        ? userAgent.Trim()
                        : DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
                {
                    Value = !string.IsNullOrWhiteSpace(correlationId)
                        ? correlationId.Trim()
                        : DBNull.Value
                });

            SqlParameter loginIdParameter = new("@LoginId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(loginIdParameter);
        }
    }
}