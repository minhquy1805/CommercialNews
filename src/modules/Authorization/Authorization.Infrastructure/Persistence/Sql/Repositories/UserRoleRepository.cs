using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Application.Contracts.Queries;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class UserRoleRepository : IUserRoleRepository
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;
        private readonly AuthorizationUnitOfWork _unitOfWork;

        public UserRoleRepository(
            AuthorizationSqlConnectionFactory connectionFactory,
            AuthorizationUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserRole?> GetActiveByUserIdAndRoleIdAsync(
            long userId,
            long roleId,
            CancellationToken cancellationToken)
        {
            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[UserRole_SelectActiveByUserIdAndRoleId]",
                    connection);

                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@RoleId", roleId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapUserRole(reader);
            }
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<UserRole> InsertAsync(
            UserRole userRole,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(userRole);

            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[UserRole_Assign]",
                    connection);

                command.Parameters.AddWithValue("@UserId", userRole.UserId);
                command.Parameters.AddWithValue("@RoleId", userRole.RoleId);
                command.Parameters.AddWithValue(
                    "@AssignedByUserId",
                    (object?)userRole.AssignedByUserId ?? DBNull.Value);

                var userRoleIdParameter = new SqlParameter("@UserRoleId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(userRoleIdParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var createdOrExisting = await GetActiveByUserIdAndRoleIdAsync(
                    userRole.UserId,
                    userRole.RoleId,
                    cancellationToken);

                if (createdOrExisting is null)
                {
                    throw new InvalidOperationException(
                        "UserRole_Assign completed but the active user-role assignment could not be reloaded.");
                }

                return createdOrExisting;
            }
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task RevokeAsync(
            long userId,
            long roleId,
            long? revokedByUserId,
            CancellationToken cancellationToken)
        {
            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[UserRole_Revoke]",
                    connection);

                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@RoleId", roleId);
                command.Parameters.AddWithValue(
                    "@RevokedByUserId",
                    (object?)revokedByUserId ?? DBNull.Value);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<IReadOnlyList<UserRoleView>> GetActiveRolesByUserIdAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            var result = new List<UserRoleView>();
            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[UserRole_SelectRolesByUserId]",
                    connection);

                command.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("RevokedAt")))
                    {
                        continue;
                    }

                    result.Add(new UserRoleView
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
                        AssignedAt = reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                        AssignedByUserId = reader.IsDBNull(reader.GetOrdinal("AssignedByUserId"))
                            ? null
                            : reader.GetInt64(reader.GetOrdinal("AssignedByUserId"))
                    });
                }

                return result;
            }
            finally
            {
                if (owned)
                {
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<IReadOnlyList<RoleUserView>> GetActiveUsersByRoleIdAsync(
            long roleId,
            CancellationToken cancellationToken)
        {
            var result = new List<RoleUserView>();
            var (connection, owned) = await GetConnectionAsync(cancellationToken);

            try
            {
                await using var command = CreateStoredProcedureCommand(
                    "[authorization].[UserRole_SelectUsersByRoleId]",
                    connection);

                command.Parameters.AddWithValue("@RoleId", roleId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("RevokedAt")))
                    {
                        continue;
                    }

                    result.Add(new RoleUserView
                    {
                        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                        PublicId = reader.GetString(reader.GetOrdinal("UserPublicId")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        FullName = reader.IsDBNull(reader.GetOrdinal("FullName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("FullName")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                        AssignedAt = reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                        AssignedByUserId = reader.IsDBNull(reader.GetOrdinal("AssignedByUserId"))
                            ? null
                            : reader.GetInt64(reader.GetOrdinal("AssignedByUserId"))
                    });
                }

                return result;
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

        private static UserRole MapUserRole(SqlDataReader reader)
        {
            return new UserRole(
                userRoleId: reader.GetInt64(reader.GetOrdinal("UserRoleId")),
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                roleId: reader.GetInt64(reader.GetOrdinal("RoleId")),
                assignedAt: reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                assignedByUserId: reader.IsDBNull(reader.GetOrdinal("AssignedByUserId"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("AssignedByUserId")),
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