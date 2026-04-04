using System.Data;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Exceptions;
using Authorization.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Repositories
{
    public sealed class PermissionRepository : IPermissionRepository
    {
        private const string PermissionInsertProc = "[authorization].[Permission_Insert]";
        private const string PermissionUpdateProc = "[authorization].[Permission_Update]";
        private const string PermissionDeleteProc = "[authorization].[Permission_Delete]";
        private const string PermissionSelectByIdProc = "[authorization].[Permission_SelectById]";
        private const string PermissionSelectByNameNormalizedProc = "[authorization].[Permission_SelectByNameNormalized]";
        private const string PermissionSelectSkipAndTakeWhereDynamicProc = "[authorization].[Permission_SelectSkipAndTakeWhereDynamic]";
        private const string PermissionGetRecordCountWhereDynamicProc = "[authorization].[Permission_GetRecordCountWhereDynamic]";

        private readonly AuthorizationUnitOfWork _unitOfWork;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly AuthorizationSqlExceptionTranslator _sqlExceptionTranslator;

        public PermissionRepository(
            AuthorizationUnitOfWork unitOfWork,
            ISqlConnectionFactory sqlConnectionFactory,
            AuthorizationSqlExceptionTranslator sqlExceptionTranslator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
            _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
        }

        public async Task<Permission?> GetByIdAsync(
            long permissionId,
            CancellationToken cancellationToken = default)
        {
            if (permissionId <= 0)
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionSelectByIdProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PermissionId", SqlDbType.BigInt)
                        {
                            Value = permissionId
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapPermission(reader);
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

        public async Task<Permission?> GetByNameNormalizedAsync(
            string nameNormalized,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nameNormalized))
            {
                return null;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionSelectByNameNormalizedProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150)
                        {
                            Value = nameNormalized.Trim()
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return MapPermission(reader);
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

        public async Task<PagedQueryResult<PermissionListResultItem>> GetPagedAsync(
            int page,
            int pageSize,
            string? query,
            string? module,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            int skip = (page - 1) * pageSize;
            List<PermissionListResultItem> items = [];
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionSelectSkipAndTakeWhereDynamicProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@NameContains", SqlDbType.NVarChar, 150)
                        {
                            Value = !string.IsNullOrWhiteSpace(query)
                                ? query.Trim()
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Module", SqlDbType.NVarChar, 100)
                        {
                            Value = !string.IsNullOrWhiteSpace(module)
                                ? module.Trim()
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit)
                        {
                            Value = isActive.HasValue
                                ? isActive.Value
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Skip", SqlDbType.Int)
                        {
                            Value = skip
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Take", SqlDbType.Int)
                        {
                            Value = pageSize
                        });

                    using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(MapPermissionListResultItem(reader));
                    }
                }

                long totalItems = await GetRecordCountWhereDynamicAsync(
                    query,
                    module,
                    isActive,
                    cancellationToken);

                return new PagedQueryResult<PermissionListResultItem>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems > int.MaxValue
                        ? int.MaxValue
                        : (int)totalItems
                };
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

        public async Task<Permission> InsertAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(permission);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionInsertProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PublicId", SqlDbType.Char, 26)
                        {
                            Value = permission.PublicId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Name", SqlDbType.NVarChar, 150)
                        {
                            Value = permission.Name
                        });

                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150)
                        {
                            Value = permission.NameNormalized
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                        {
                            Value = (object?)permission.Description ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Module", SqlDbType.NVarChar, 100)
                        {
                            Value = (object?)permission.Module ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsSystem", SqlDbType.Bit)
                        {
                            Value = permission.IsSystem
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit)
                        {
                            Value = permission.IsActive
                        });

                    command.Parameters.Add(
                        new SqlParameter("@CreatedByUserId", SqlDbType.BigInt)
                        {
                            Value = (object?)permission.CreatedByUserId ?? DBNull.Value
                        });

                    SqlParameter permissionIdParameter = new("@PermissionId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(permissionIdParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    if (permissionIdParameter.Value is null || permissionIdParameter.Value == DBNull.Value)
                    {
                        throw new InvalidOperationException("Permission_Insert did not return PermissionId.");
                    }

                    long createdPermissionId = Convert.ToInt64(permissionIdParameter.Value);

                    Permission? createdPermission = await GetByIdAsync(
                        createdPermissionId,
                        cancellationToken);

                    if (createdPermission is null)
                    {
                        throw new InvalidOperationException(
                            $"Permission with id {createdPermissionId} was inserted but could not be reloaded.");
                    }

                    return createdPermission;
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

        public async Task<Permission> UpdateAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(permission);

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionUpdateProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PermissionId", SqlDbType.BigInt)
                        {
                            Value = permission.PermissionId
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Name", SqlDbType.NVarChar, 150)
                        {
                            Value = permission.Name
                        });

                    command.Parameters.Add(
                        new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 150)
                        {
                            Value = permission.NameNormalized
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Description", SqlDbType.NVarChar, 500)
                        {
                            Value = (object?)permission.Description ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Module", SqlDbType.NVarChar, 100)
                        {
                            Value = (object?)permission.Module ?? DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit)
                        {
                            Value = permission.IsActive
                        });

                    command.Parameters.Add(
                        new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                        {
                            Value = (object?)permission.UpdatedByUserId ?? DBNull.Value
                        });

                    SqlParameter affectedRowsParameter = new("@AffectedRows", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(affectedRowsParameter);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    Permission? updatedPermission = await GetByIdAsync(
                        permission.PermissionId,
                        cancellationToken);

                    if (updatedPermission is null)
                    {
                        throw new InvalidOperationException(
                            $"Permission with id {permission.PermissionId} was updated but could not be reloaded.");
                    }

                    return updatedPermission;
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

        public async Task<bool> DeleteAsync(
            long permissionId,
            CancellationToken cancellationToken = default)
        {
            if (permissionId <= 0)
            {
                return false;
            }

            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionDeleteProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@PermissionId", SqlDbType.BigInt)
                        {
                            Value = permissionId
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

        private async Task<long> GetRecordCountWhereDynamicAsync(
            string? query,
            string? module,
            bool? isActive,
            CancellationToken cancellationToken)
        {
            SqlConnection? ownedConnection = null;

            try
            {
                (SqlCommand command, SqlConnection? connection) =
                    await CreateCommandAsync(PermissionGetRecordCountWhereDynamicProc, cancellationToken);

                ownedConnection = connection;

                using (command)
                {
                    command.Parameters.Add(
                        new SqlParameter("@NameContains", SqlDbType.NVarChar, 150)
                        {
                            Value = !string.IsNullOrWhiteSpace(query)
                                ? query.Trim()
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@Module", SqlDbType.NVarChar, 100)
                        {
                            Value = !string.IsNullOrWhiteSpace(module)
                                ? module.Trim()
                                : DBNull.Value
                        });

                    command.Parameters.Add(
                        new SqlParameter("@IsActive", SqlDbType.Bit)
                        {
                            Value = isActive.HasValue
                                ? isActive.Value
                                : DBNull.Value
                        });

                    object? scalar = await command.ExecuteScalarAsync(cancellationToken);

                    if (scalar is null || scalar is DBNull)
                    {
                        return 0;
                    }

                    return Convert.ToInt64(scalar);
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

        private static Permission MapPermission(SqlDataReader reader)
        {
            return Permission.Rehydrate(
                permissionId: reader.GetInt64(reader.GetOrdinal("PermissionId")),
                publicId: reader.GetString(reader.GetOrdinal("PublicId")),
                name: reader.GetString(reader.GetOrdinal("Name")),
                nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
                description: ReadNullableString(reader, "Description"),
                module: ReadNullableString(reader, "Module"),
                isSystem: reader.GetBoolean(reader.GetOrdinal("IsSystem")),
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                createdByUserId: ReadNullableInt64(reader, "CreatedByUserId"),
                updatedByUserId: ReadNullableInt64(reader, "UpdatedByUserId"));
        }

        private static PermissionListResultItem MapPermissionListResultItem(SqlDataReader reader)
        {
            return new PermissionListResultItem
            {
                PermissionId = reader.GetInt64(reader.GetOrdinal("PermissionId")),
                PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                NameNormalized = reader.GetString(reader.GetOrdinal("NameNormalized")),
                Description = ReadNullableString(reader, "Description"),
                Module = ReadNullableString(reader, "Module"),
                IsSystem = reader.GetBoolean(reader.GetOrdinal("IsSystem")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                CreatedByUserId = ReadNullableInt64(reader, "CreatedByUserId"),
                UpdatedByUserId = ReadNullableInt64(reader, "UpdatedByUserId")
            };
        }

        private static string? ReadNullableString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static long? ReadNullableInt64(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
        }
    }
}