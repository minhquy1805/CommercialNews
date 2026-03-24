using Authorization.Application.Contracts.Ports;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Authorization.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class RoleRepository : IRoleRepository
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;

        public RoleRepository(AuthorizationSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Role?> GetByIdAsync(
            long roleId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("[authorization].[Role_SelectById]", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@RoleId", roleId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapRole(reader);
        }

        private static Role MapRole(SqlDataReader reader)
        {
            return new Role(
                roleId: reader.GetInt64(reader.GetOrdinal("RoleId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                name: reader.GetString(reader.GetOrdinal("Name")),
                nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
                description: reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                isSystem: reader.GetBoolean(reader.GetOrdinal("IsSystem")),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                createdByUserId: reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("CreatedByUserId")),
                updatedByUserId: reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("UpdatedByUserId"))
            );
        }
    }
}