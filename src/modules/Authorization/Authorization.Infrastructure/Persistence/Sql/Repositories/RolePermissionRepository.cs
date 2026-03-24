using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Queries;
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

        public async Task<IReadOnlyList<RolePermissionView>> GetActivePermissionsByRoleIdAsync(
            long roleId,
            CancellationToken cancellationToken)
        {
            var result = new List<RolePermissionView>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[RolePermission_SelectPermissionsByRoleId]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@RoleId", roleId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(reader.GetOrdinal("RevokedAt")))
                {
                    continue;
                }

                result.Add(new RolePermissionView
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
                    IsActive = reader.GetBoolean(reader.GetOrdinal("PermissionIsActive")),
                    GrantedAt = reader.GetDateTime(reader.GetOrdinal("GrantedAt")),
                    GrantedByUserId = reader.IsDBNull(reader.GetOrdinal("GrantedByUserId"))
                        ? null
                        : reader.GetInt64(reader.GetOrdinal("GrantedByUserId"))
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<PermissionRoleView>> GetActiveRolesByPermissionIdAsync(
            long permissionId,
            CancellationToken cancellationToken)
        {
            var result = new List<PermissionRoleView>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[RolePermission_SelectRolesByPermissionId]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PermissionId", permissionId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(reader.GetOrdinal("RevokedAt")))
                {
                    continue;
                }

                result.Add(new PermissionRoleView
                {
                    RoleId = reader.GetInt64(reader.GetOrdinal("RoleId")),
                    PublicId = reader.GetString(reader.GetOrdinal("RolePublicId")),
                    Name = reader.GetString(reader.GetOrdinal("RoleName")),
                    NameNormalized = reader.GetString(reader.GetOrdinal("RoleNameNormalized")),
                    Description = reader.IsDBNull(reader.GetOrdinal("RoleDescription"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("RoleDescription")),
                    IsSystem = reader.GetBoolean(reader.GetOrdinal("RoleIsSystem")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("RoleIsActive")),
                    GrantedAt = reader.GetDateTime(reader.GetOrdinal("GrantedAt")),
                    GrantedByUserId = reader.IsDBNull(reader.GetOrdinal("GrantedByUserId"))
                        ? null
                        : reader.GetInt64(reader.GetOrdinal("GrantedByUserId"))
                });
            }

            return result;
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