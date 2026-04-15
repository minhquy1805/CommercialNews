using System.Data;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Exceptions;
using Authorization.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Repositories
{
    public sealed class UserRoleRepository : IUserRoleRepository
    {
        private const string UserRoleAssignProc = "[authorization].[UserRole_Assign]";
        private const string UserRoleRevokeProc = "[authorization].[UserRole_Revoke]";
        private const string UserRoleSelectActiveByUserIdAndRoleIdProc = "[authorization].[UserRole_SelectActiveByUserIdAndRoleId]";
        private const string UserRoleSelectRolesByUserIdProc = "[authorization].[UserRole_SelectRolesByUserId]";
        private const string UserRoleSelectUsersByRoleIdProc = "[authorization].[UserRole_SelectUsersByRoleId]";

        private readonly AuthorizationUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly AuthorizationSqlExceptionTranslator _sqlExceptionTranslator;

        public UserRoleRepository(
            AuthorizationUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            AuthorizationSqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<UserRole?> GetActiveByUserIdAndRoleIdAsync(
            long userId,
            long roleId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0 || roleId <= 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserRoleSelectActiveByUserIdAndRoleIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = userId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@RoleId", SqlDbType.BigInt)
                        {
                            Value = roleId
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapUserRole(reader);
                }
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        public async Task<UserRole> InsertAsync(
            UserRole userRole,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userRole);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserRoleAssignProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = userRole.UserId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@RoleId", SqlDbType.BigInt)
                        {
                            Value = userRole.RoleId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@AssignedByUserId", SqlDbType.BigInt)
                        {
                            Value = (object?)userRole.AssignedByUserId ?? DBNull.Value
                        });

                    SqlParameter userRoleIdParameter = new("@UserRoleId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(userRoleIdParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                UserRole? createdOrExisting = await GetActiveByUserIdAndRoleIdAsync(
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
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        public async Task<bool> RevokeAsync(
            long userId,
            long roleId,
            long? revokedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0 || roleId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserRoleRevokeProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = userId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@RoleId", SqlDbType.BigInt)
                        {
                            Value = roleId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@RevokedByUserId", SqlDbType.BigInt)
                        {
                            Value = (object?)revokedByUserId ?? DBNull.Value
                        });

                    SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(affectedRowsParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    int affectedRows = affectedRowsParameter.Value is DBNull
                        ? 0
                        : Convert.ToInt32(affectedRowsParameter.Value);

                    return affectedRows > 0;
                }
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        public async Task<IReadOnlyList<UserRoleListResultItem>> GetActiveRolesByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return Array.Empty<UserRoleListResultItem>();
            }

            List<UserRoleListResultItem> result = [];
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserRoleSelectRolesByUserIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@UserId", SqlDbType.BigInt)
                        {
                            Value = userId
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        result.Add(MapUserRoleListResultItem(reader));
                    }

                    return result;
                }
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        public async Task<IReadOnlyList<RoleUserListResultItem>> GetActiveUsersByRoleIdAsync(
            long roleId,
            CancellationToken cancellationToken = default)
        {
            if (roleId <= 0)
            {
                return Array.Empty<RoleUserListResultItem>();
            }

            List<RoleUserListResultItem> result = [];
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(UserRoleSelectUsersByRoleIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@RoleId", SqlDbType.BigInt)
                        {
                            Value = roleId
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        result.Add(MapRoleUserListResultItem(reader));
                    }

                    return result;
                }
            }
            catch (SqlException exception)
            {
                throw _sqlExceptionTranslator.Translate(exception);
            }
            finally
            {
                if (ownedConnection is not null)
                {
                    await ownedConnection.DisposeAsync();
                }
            }
        }

        private async Task<(SqlCommand Command, SqlConnection? OwnedConnection)> CreateCommandAsync(
            string storedProcedureName,
            CancellationToken cancellationToken)
        {
            if (_unitOfWork.HasActiveConnection)
            {
                SqlCommand ambientCommand = _unitOfWork.Connection.CreateCommand();
                ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                    ? _unitOfWork.Transaction
                    : null;
                ambientCommand.CommandText = storedProcedureName;
                ambientCommand.CommandType = CommandType.StoredProcedure;

                return (ambientCommand, null);
            }

            SqlConnection ownedConnection = _sqlConnectionFactory.CreateConnection();
            await ownedConnection.OpenAsync(cancellationToken);

            SqlCommand command = ownedConnection.CreateCommand();
            command.CommandText = storedProcedureName;
            command.CommandType = CommandType.StoredProcedure;

            return (command, ownedConnection);
        }

        private static UserRole MapUserRole(SqlDataReader reader)
        {
            return UserRole.Rehydrate(
                userRoleId: reader.GetInt64(reader.GetOrdinal("UserRoleId")),
                userId: reader.GetInt64(reader.GetOrdinal("UserId")),
                roleId: reader.GetInt64(reader.GetOrdinal("RoleId")),
                assignedAt: reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                assignedByUserId: ReadNullableInt64(reader, "AssignedByUserId"),
                revokedAt: ReadNullableDateTime(reader, "RevokedAt"),
                revokedByUserId: ReadNullableInt64(reader, "RevokedByUserId"));
        }

        private static UserRoleListResultItem MapUserRoleListResultItem(SqlDataReader reader)
        {
            return new UserRoleListResultItem
            {
                RoleId = reader.GetInt64(reader.GetOrdinal("RoleId")),
                PublicId = reader.GetString(reader.GetOrdinal("RolePublicId")),
                Name = reader.GetString(reader.GetOrdinal("RoleName")),
                NameNormalized = reader.GetString(reader.GetOrdinal("RoleNameNormalized")),
                Description = ReadNullableString(reader, "RoleDescription"),
                IsSystem = reader.GetBoolean(reader.GetOrdinal("RoleIsSystem")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("RoleIsActive")),
                AssignedAt = reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                AssignedByUserId = ReadNullableInt64(reader, "AssignedByUserId")
            };
        }

        private static RoleUserListResultItem MapRoleUserListResultItem(SqlDataReader reader)
        {
            return new RoleUserListResultItem
            {
                UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                PublicId = reader.GetString(reader.GetOrdinal("UserPublicId")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                FullName = ReadNullableString(reader, "FullName"),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                AssignedAt = reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                AssignedByUserId = ReadNullableInt64(reader, "AssignedByUserId")
            };
        }

        private static string? ReadNullableString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        private static long? ReadNullableInt64(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
        }
    }
}