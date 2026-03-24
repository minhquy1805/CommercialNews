using System.Data;
using Authorization.Application.Contracts.Ports;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Sql;
using Microsoft.Data.SqlClient;

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

            await using var command = new SqlCommand(
                "[authorization].[Role_SelectById]",
                connection)
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

        public async Task<Role?> GetByNameNormalizedAsync(
            string nameNormalized,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameNormalized);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Role_SelectByNameNormalized]",
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

            return MapRole(reader);
        }

        public async Task<Role> InsertAsync(
            Role role,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(role);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Role_Insert]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PublicId", role.PublicId);
            command.Parameters.AddWithValue("@Name", role.Name);
            command.Parameters.AddWithValue("@NameNormalized", role.NameNormalized);
            command.Parameters.AddWithValue("@Description", (object?)role.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsSystem", role.IsSystem);
            command.Parameters.AddWithValue("@CreatedByUserId", (object?)role.CreatedByUserId ?? DBNull.Value);

            var roleIdParameter = new SqlParameter("@RoleId", SqlDbType.BigInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(roleIdParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (roleIdParameter.Value is null || roleIdParameter.Value == DBNull.Value)
            {
                throw new InvalidOperationException("Role_Insert did not return RoleId.");
            }

            var createdRoleId = Convert.ToInt64(roleIdParameter.Value);

            var createdRole = await GetByIdAsync(createdRoleId, cancellationToken);

            if (createdRole is null)
            {
                throw new InvalidOperationException(
                    $"Role with id {createdRoleId} was inserted but could not be reloaded.");
            }

            return createdRole;
        }

        public async Task<Role> UpdateAsync(
            Role role,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(role);

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "[authorization].[Role_Update]",
                connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@RoleId", role.RoleId);
            command.Parameters.AddWithValue("@Name", role.Name);
            command.Parameters.AddWithValue("@NameNormalized", role.NameNormalized);
            command.Parameters.AddWithValue("@Description", (object?)role.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", role.IsActive);
            command.Parameters.AddWithValue("@UpdatedByUserId", (object?)role.UpdatedByUserId ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var updatedRole = await GetByIdAsync(role.RoleId, cancellationToken);

            if (updatedRole is null)
            {
                throw new InvalidOperationException(
                    $"Role with id {role.RoleId} was updated but could not be reloaded.");
            }

            return updatedRole;
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