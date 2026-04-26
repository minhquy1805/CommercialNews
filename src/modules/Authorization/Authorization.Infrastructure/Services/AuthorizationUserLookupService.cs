using System.Data;
using Authorization.Application.Models;
using Authorization.Application.Ports.Services;
using Authorization.Infrastructure.Persistence.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Services;

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

        await using var connection = _sqlConnectionFactory.CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.CommandType = CommandType.Text;
            command.Parameters.Add(
                new SqlParameter("@UserId", SqlDbType.BigInt)
                {
                    Value = userId
                });

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && result is not DBNull;
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    public async Task<AuthorizationUserLookupResult?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)
                [UserId],
                [PublicId],
                [Email]
            FROM [identity].[UserAccount]
            WHERE [Email] = @Email;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.CommandType = CommandType.Text;
            command.Parameters.Add(
                new SqlParameter("@Email", SqlDbType.NVarChar, 256)
                {
                    Value = email.Trim()
                });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new AuthorizationUserLookupResult(
                UserId: reader.GetInt64(reader.GetOrdinal("UserId")),
                PublicId: reader.GetString(reader.GetOrdinal("PublicId")),
                Email: reader.GetString(reader.GetOrdinal("Email")));
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }
}