using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Identity.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class LoginHistoryRepository : ILoginHistoryRepository
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;
        private readonly IdentityUnitOfWork _unitOfWork;

        public LoginHistoryRepository(
            IdentitySqlConnectionFactory connectionFactory,
            IdentityUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task InsertAsync(
            long? userId,
            string? emailNormalizedAttempted,
            bool succeeded,
            string? failureReason,
            string? ipAddress,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                await using var command = new SqlCommand(
                    "[identity].[LoginHistory_Insert]",
                    _unitOfWork.Connection,
                    _unitOfWork.Transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };

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
                return;
            }

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var standaloneCommand = new SqlCommand(
                "[identity].[LoginHistory_Insert]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(
                standaloneCommand,
                userId,
                emailNormalizedAttempted,
                succeeded,
                failureReason,
                ipAddress,
                userAgent,
                correlationId);

            await standaloneCommand.ExecuteNonQueryAsync(cancellationToken);
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
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Value = (object?)userId ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@EmailNormalizedAttempted", SqlDbType.NVarChar, 320)
            {
                Value = (object?)emailNormalizedAttempted ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@Succeeded", SqlDbType.Bit)
            {
                Value = succeeded
            });

            command.Parameters.Add(new SqlParameter("@FailureReason", SqlDbType.NVarChar, 100)
            {
                Value = (object?)failureReason ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@IpAddress", SqlDbType.NVarChar, 45)
            {
                Value = (object?)ipAddress ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
            {
                Value = (object?)userAgent ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)correlationId ?? DBNull.Value
            });

            var loginIdParameter = new SqlParameter("@LoginId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(loginIdParameter);
        }
    }
}
