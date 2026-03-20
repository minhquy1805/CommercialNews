using System.Data;
using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class RefreshTokenRotationService : IRefreshTokenRotationService
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;

        public RefreshTokenRotationService(IdentitySqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<long> RotateAsync(
            byte[] currentTokenHash,
            byte[] newTokenHash,
            DateTime newExpiresAtUtc,
            string? createdIp,
            string? userAgent,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[identity].[RefreshToken_Rotate]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@CurrentTokenHash", SqlDbType.VarBinary, 32)
            {
                Value = currentTokenHash
            });

            command.Parameters.Add(new SqlParameter("@NewTokenHash", SqlDbType.VarBinary, 32)
            {
                Value = newTokenHash
            });

            command.Parameters.Add(new SqlParameter("@NewExpiresAt", SqlDbType.DateTime2)
            {
                Value = newExpiresAtUtc
            });

            command.Parameters.Add(new SqlParameter("@CreatedIp", SqlDbType.NVarChar, 45)
            {
                Value = (object?)createdIp ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300)
            {
                Value = (object?)userAgent ?? DBNull.Value
            });

            command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)correlationId ?? DBNull.Value
            });

            var newRefreshTokenIdParameter = new SqlParameter("@NewRefreshTokenId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(newRefreshTokenIdParameter);

            var userIdParameter = new SqlParameter("@UserId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(userIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return Convert.ToInt64(userIdParameter.Value);
        }
    }
}

