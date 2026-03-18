using Identity.Application.Contracts.Ports;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Identity.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class IdentityVerificationRepository : IIdentityVerificationRepository
    {
        private readonly IdentitySqlConnectionFactory _connectionFactory;

        public IdentityVerificationRepository(IdentitySqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<long> VerifyEmailByTokenHashAsync(
            byte[] tokenHash,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[identity].[UserAccount_VerifyEmailByTokenHash]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.VarBinary, 32)
            {
                Value = tokenHash
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
