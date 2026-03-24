using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class PermissionRepository : IPermissionRepository
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;

        public PermissionRepository(AuthorizationSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Permission?> GetByIdAsync(
            long permissionId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Permission_SelectById]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PermissionId", permissionId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapPermission(reader);
        }

        private static Permission MapPermission(SqlDataReader reader)
        {
            return new Permission(
                permissionId: reader.GetInt64(reader.GetOrdinal("PermissionId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                name: reader.GetString(reader.GetOrdinal("Name")),
                nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
                description: reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Description")),
                module: reader.IsDBNull(reader.GetOrdinal("Module"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Module")),
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