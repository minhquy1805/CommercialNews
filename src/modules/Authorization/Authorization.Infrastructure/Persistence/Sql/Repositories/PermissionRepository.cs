using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Sql;
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

        public async Task<Permission?> GetByNameNormalizedAsync(
            string nameNormalized,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameNormalized);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Permission_SelectByNameNormalized]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@NameNormalized", nameNormalized);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapPermission(reader);
        }

        public async Task<Permission> InsertAsync(
            Permission permission,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(permission);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Permission_Insert]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PublicId", permission.PublicId);
            command.Parameters.AddWithValue("@Name", permission.Name);
            command.Parameters.AddWithValue("@NameNormalized", permission.NameNormalized);
            command.Parameters.AddWithValue("@Description", (object?)permission.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Module", (object?)permission.Module ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsSystem", permission.IsSystem);
            command.Parameters.AddWithValue("@CreatedByUserId", (object?)permission.CreatedByUserId ?? DBNull.Value);

            var permissionIdParameter = new SqlParameter("@PermissionId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(permissionIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (permissionIdParameter.Value is null || permissionIdParameter.Value == DBNull.Value)
            {
                throw new InvalidOperationException("Permission_Insert did not return PermissionId.");
            }

            var createdPermissionId = Convert.ToInt64(permissionIdParameter.Value);

            var createdPermission = await GetByIdAsync(createdPermissionId, cancellationToken);

            if (createdPermission is null)
            {
                throw new InvalidOperationException(
                    $"Permission with id {createdPermissionId} was inserted but could not be reloaded.");
            }

            return createdPermission;
        }

        public async Task<Permission> UpdateAsync(
            Permission permission,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(permission);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Permission_Update]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PermissionId", permission.PermissionId);
            command.Parameters.AddWithValue("@Name", permission.Name);
            command.Parameters.AddWithValue("@NameNormalized", permission.NameNormalized);
            command.Parameters.AddWithValue("@Description", (object?)permission.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Module", (object?)permission.Module ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", permission.IsActive);
            command.Parameters.AddWithValue("@UpdatedByUserId", (object?)permission.UpdatedByUserId ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var updatedPermission = await GetByIdAsync(permission.PermissionId, cancellationToken);

            if (updatedPermission is null)
            {
                throw new InvalidOperationException(
                    $"Permission with id {permission.PermissionId} was updated but could not be reloaded.");
            }

            return updatedPermission;
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