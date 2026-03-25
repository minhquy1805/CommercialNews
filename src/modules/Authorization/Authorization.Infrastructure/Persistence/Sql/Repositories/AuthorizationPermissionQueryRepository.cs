using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Queries;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class AuthorizationPermissionQueryRepository : IAuthorizationPermissionQueryRepository
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;

        public AuthorizationPermissionQueryRepository(AuthorizationSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<EffectivePermissionView>> GetEffectivePermissionsByUserIdAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            var result = new List<EffectivePermissionView>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Authorization_SelectEffectivePermissionsByUserId]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new EffectivePermissionView
                {
                    PermissionId = reader.GetInt64(reader.GetOrdinal("PermissionId")),
                    PublicId = reader.GetString(reader.GetOrdinal("PermissionPublicId")),
                    Name = reader.GetString(reader.GetOrdinal("PermissionName")),
                    NameNormalized = reader.GetString(reader.GetOrdinal("PermissionNameNormalized")),
                    Description = reader.IsDBNull(reader.GetOrdinal("PermissionDescription"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PermissionDescription")),
                    Module = reader.IsDBNull(reader.GetOrdinal("PermissionModule"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("PermissionModule")),
                    IsSystem = reader.GetBoolean(reader.GetOrdinal("PermissionIsSystem")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("PermissionIsActive"))
                });
            }

            return result;
        }
    }
}