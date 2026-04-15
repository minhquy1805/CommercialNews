using Authorization.Application.Ports.Services;
using Authorization.Infrastructure.Persistence.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Services
{
    public sealed class AuthorizationUserLookupService : IAuthorizationUserLookupService
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly AuthorizationSqlExceptionTranslator _sqlExceptionTranslator;

        public AuthorizationUserLookupService(
            ISqlConnectionFactory sqlConnectionFactory,
            AuthorizationSqlExceptionTranslator sqlExceptionTranslator)
        {
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<bool> ExistsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return false;
            }

            const string sql = """
                SELECT TOP (1) 1
                FROM [identity].[UserAccount]
                WHERE [UserId] = @UserId;
                """;

            await using SqlConnection connection = _sqlConnectionFactory.CreateConnection();

            try
            {
                await connection.OpenAsync(cancellationToken);

                await using SqlCommand command = new(sql, connection);
                command.Parameters.Add(
                    new SqlParameter("@UserId", System.Data.SqlDbType.BigInt)
                    {
                        Value = userId
                    });

                object? result = await command.ExecuteScalarAsync(cancellationToken);
                return result is not null && result is not DBNull;
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
        }
    }
}