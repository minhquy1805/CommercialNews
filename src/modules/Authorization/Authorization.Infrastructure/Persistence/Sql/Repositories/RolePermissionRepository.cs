using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;

        public RolePermissionRepository(AuthorizationSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<RolePermission?> GetActiveByRoleIdAndPermissionIdAsync(
            long roleId,
            long permissionId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[RolePermission_SelectActiveByRoleIdAndPermissionId]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@RoleId", roleId);
            command.Parameters.AddWithValue("@PermissionId", permissionId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapRolePermission(reader);
        }

        public async Task<RolePermission> InsertAsync(
            RolePermission rolePermission,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(rolePermission);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[RolePermission_Grant]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@RoleId", rolePermission.RoleId);
            command.Parameters.AddWithValue("@PermissionId", rolePermission.PermissionId);
            command.Parameters.AddWithValue(
                "@GrantedByUserId",
                (object?)rolePermission.GrantedByUserId ?? DBNull.Value);

            var rolePermissionIdParameter = new SqlParameter("@RolePermissionId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(rolePermissionIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var createdOrExisting = await GetActiveByRoleIdAndPermissionIdAsync(
                rolePermission.RoleId,
                rolePermission.PermissionId,
                cancellationToken);

            if (createdOrExisting is null)
            {
                throw new InvalidOperationException(
                    "RolePermission_Grant completed but the active role-permission grant could not be reloaded.");
            }

            return createdOrExisting;
        }

        public async Task RevokeAsync(
            long roleId,
            long permissionId,
            long? revokedByUserId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[RolePermission_Revoke]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@RoleId", roleId);
            command.Parameters.AddWithValue("@PermissionId", permissionId);
            command.Parameters.AddWithValue(
                "@RevokedByUserId",
                (object?)revokedByUserId ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static RolePermission MapRolePermission(SqlDataReader reader)
        {
            return new RolePermission(
                rolePermissionId: reader.GetInt64(reader.GetOrdinal("RolePermissionId")),
                roleId: reader.GetInt64(reader.GetOrdinal("RoleId")),
                permissionId: reader.GetInt64(reader.GetOrdinal("PermissionId")),
                grantedAt: reader.GetDateTime(reader.GetOrdinal("GrantedAt")),
                grantedByUserId: reader.IsDBNull(reader.GetOrdinal("GrantedByUserId"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("GrantedByUserId")),
                revokedAt: reader.IsDBNull(reader.GetOrdinal("RevokedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("RevokedAt")),
                revokedByUserId: reader.IsDBNull(reader.GetOrdinal("RevokedByUserId"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("RevokedByUserId"))
            );
        }
    }
}