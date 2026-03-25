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
        private readonly AuthorizationUnitOfWork _unitOfWork;

        public PermissionRepository(
            AuthorizationSqlConnectionFactory connectionFactory,
            AuthorizationUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<Permission?> GetByIdAsync(
            long permissionId,
            CancellationToken cancellationToken)
        {
            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[Permission_SelectById]",
                    connection);

                command.Parameters.AddWithValue("@PermissionId", permissionId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapPermission(reader);
            }
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<Permission?> GetByNameNormalizedAsync(
            string nameNormalized,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameNormalized);

            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[Permission_SelectByNameNormalized]",
                    connection);

                command.Parameters.AddWithValue("@NameNormalized", nameNormalized);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapPermission(reader);
            }
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<Permission> InsertAsync(
            Permission permission,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(permission);

            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[Permission_Insert]",
                    connection);

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
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<Permission> UpdateAsync(
            Permission permission,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(permission);

            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[Permission_Update]",
                    connection);

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
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        private async Task<(SqlConnection Connection, bool Owned)> GetConnectionAsync(
            CancellationToken cancellationToken)
        {
            if (_unitOfWork.HasActiveConnection)
            {
                return (_unitOfWork.Connection, false);
            }

            var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            return (connection, true);
        }

        private SqlCommand CreateStoredProcedureCommand(
            string procedureName,
            SqlConnection connection)
        {
            return new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                Transaction = _unitOfWork.HasActiveTransaction
                    ? _unitOfWork.Transaction
                    : null
            };
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