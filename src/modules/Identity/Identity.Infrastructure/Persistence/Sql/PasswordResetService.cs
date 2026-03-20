using System.Data;
using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class PasswordResetService : IPasswordResetService
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;

        public PasswordResetService(IdentitySqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<long> ResetPasswordByTokenHashAsync(
            byte[] tokenHash,
            string newPasswordHash,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[identity].[UserAccount_ResetPasswordByTokenHash]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
            {
                Value = tokenHash
            });

            command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
            {
                Value = newPasswordHash
            });

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

