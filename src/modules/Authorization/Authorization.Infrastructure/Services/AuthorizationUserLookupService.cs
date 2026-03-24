using Authorization.Application.Contracts.Ports;
using Authorization.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Services
{
    public sealed class AuthorizationUserLookupService : IAuthorizationUserLookupService
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;

        public AuthorizationUserLookupService(AuthorizationSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> ExistsAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT TOP (1) 1
                FROM [identity].[UserAccount]
                WHERE [UserId] = @UserId;
                """;

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null;
        }
    }
}