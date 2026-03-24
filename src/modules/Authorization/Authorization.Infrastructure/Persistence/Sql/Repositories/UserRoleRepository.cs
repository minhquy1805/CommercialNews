using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class UserRoleRepository : IUserRoleRepository
    {
        private readonly AuthorizationSqlConnectionFactory _connectionFactory;

        public UserRoleRepository(AuthorizationSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserRole?> GetActiveByUserIdAndRoleIdAsync(
            long userId,
            long roleId,
            CancellationToken cancellationToken)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[UserRole_SelectActiveByUserIdAndRoleId]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@RoleId", roleId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapUserRole(reader);
        }

        public async Task<UserRole> InsertAsync(
            UserRole userRole,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(userRole);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[UserRole_Assign]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

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