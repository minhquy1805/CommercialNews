using System.Data;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Infrastructure.Persistence.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Repositories;

public sealed class AuthorizationPermissionQueryRepository : IAuthorizationPermissionQueryRepository
{
    private const string AuthorizationSelectEffectivePermissionsByUserIdProc =
        "[authorization].[Authorization_SelectEffectivePermissionsByUserId]";

    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly AuthorizationSqlExceptionTranslator _sqlExceptionTranslator;

    public AuthorizationPermissionQueryRepository(
        ISqlConnectionFactory sqlConnectionFactory,
        AuthorizationSqlExceptionTranslator sqlExceptionTranslator)
    {
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<IReadOnlyList<EffectivePermissionListResultItem>> GetEffectivePermissionsByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return Array.Empty<EffectivePermissionListResultItem>();
        }

        List<EffectivePermissionListResultItem> result = [];

        await using var connection = _sqlConnectionFactory.CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = AuthorizationSelectEffectivePermissionsByUserIdProc;
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                new SqlParameter("@UserId", SqlDbType.BigInt)
                {
                    Value = userId
                });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(MapEffectivePermissionListResultItem(reader));
            }

            return result;
        }
        catch (SqlException exception)
        {
            throw _sqlExceptionTranslator.Translate(exception);
        }
    }

    private static EffectivePermissionListResultItem MapEffectivePermissionListResultItem(SqlDataReader reader)
    {
        return new EffectivePermissionListResultItem
        {
            PermissionId = reader.GetInt64(reader.GetOrdinal("PermissionId")),
            PublicId = reader.GetString(reader.GetOrdinal("PermissionPublicId")),
            Key = reader.GetString(reader.GetOrdinal("PermissionKey")),
            KeyNormalized = reader.GetString(reader.GetOrdinal("PermissionKeyNormalized")),
            Description = ReadNullableString(reader, "PermissionDescription"),
            Module = ReadNullableString(reader, "PermissionModule"),
            Action = ReadNullableString(reader, "PermissionAction"),
            IsSystem = reader.GetBoolean(reader.GetOrdinal("PermissionIsSystem")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("PermissionIsActive"))
        };
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}