using System.Data;
using Authorization.Application.Models.QueryModels;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Infrastructure.Persistence.Exceptions;
using Authorization.Infrastructure.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using Microsoft.Data.SqlClient;

namespace Authorization.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private const string RoleInsertProc = "[authorization].[Role_Insert]";
    private const string RoleUpdateProc = "[authorization].[Role_Update]";
    private const string RoleDeleteProc = "[authorization].[Role_Delete]";
    private const string RoleSelectByIdProc = "[authorization].[Role_SelectById]";
    private const string RoleSelectByNameNormalizedProc = "[authorization].[Role_SelectByNameNormalized]";
    private const string RoleSelectSkipAndTakeWhereDynamicProc = "[authorization].[Role_SelectSkipAndTakeWhereDynamic]";
    private const string RoleGetRecordCountWhereDynamicProc = "[authorization].[Role_GetRecordCountWhereDynamic]";

    private readonly AuthorizationUnitOfWork _unitOfWork;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly AuthorizationSqlExceptionTranslator _sqlExceptionTranslator;

    public RoleRepository(
        AuthorizationUnitOfWork unitOfWork,
        ISqlConnectionFactory sqlConnectionFactory,
        AuthorizationSqlExceptionTranslator sqlExceptionTranslator)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _sqlExceptionTranslator = sqlExceptionTranslator ?? throw new ArgumentNullException(nameof(sqlExceptionTranslator));
    }

    public async Task<Role?> GetByIdAsync(
        long roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            return null;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RoleSelectByIdProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = roleId
                    });

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapRole(reader);
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

    public async Task<Role?> GetByNameNormalizedAsync(
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
            var (command, connection) = await CreateCommandAsync(
                RoleSelectByNameNormalizedProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 80)
                    {
                        Value = nameNormalized.Trim()
                    });

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return MapRole(reader);
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

    public async Task<PagedQueryResult<RoleListResultItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? query,
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

        var skip = (page - 1) * pageSize;
        List<RoleListResultItem> items = [];
        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RoleSelectSkipAndTakeWhereDynamicProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@NameContains", SqlDbType.NVarChar, 80)
                    {
                        Value = !string.IsNullOrWhiteSpace(query)
                            ? query.Trim()
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

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapRoleListResultItem(reader));
                }
            }

            var totalItems = await GetRecordCountWhereDynamicAsync(
                query,
                isActive,
                cancellationToken);

            return new PagedQueryResult<RoleListResultItem>
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

    public async Task<Role> InsertAsync(
        Role role,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RoleInsertProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@PublicId", SqlDbType.Char, 26)
                    {
                        Value = role.PublicId
                    });

                command.Parameters.Add(
                    new SqlParameter("@Name", SqlDbType.NVarChar, 80)
                    {
                        Value = role.Name
                    });

                command.Parameters.Add(
                    new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 80)
                    {
                        Value = role.NameNormalized
                    });

                command.Parameters.Add(
                    new SqlParameter("@DisplayName", SqlDbType.NVarChar, 120)
                    {
                        Value = (object?)role.DisplayName ?? DBNull.Value
                    });

                command.Parameters.Add(
                    new SqlParameter("@Description", SqlDbType.NVarChar, 300)
                    {
                        Value = (object?)role.Description ?? DBNull.Value
                    });

                command.Parameters.Add(
                    new SqlParameter("@IsSystem", SqlDbType.Bit)
                    {
                        Value = role.IsSystem
                    });

                command.Parameters.Add(
                    new SqlParameter("@IsActive", SqlDbType.Bit)
                    {
                        Value = role.IsActive
                    });

                command.Parameters.Add(
                    new SqlParameter("@CreatedByUserId", SqlDbType.BigInt)
                    {
                        Value = (object?)role.CreatedByUserId ?? DBNull.Value
                    });

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

    public async Task<Role> UpdateAsync(
        Role role,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RoleUpdateProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = role.RoleId
                    });

                command.Parameters.Add(
                    new SqlParameter("@Name", SqlDbType.NVarChar, 80)
                    {
                        Value = role.Name
                    });

                command.Parameters.Add(
                    new SqlParameter("@NameNormalized", SqlDbType.NVarChar, 80)
                    {
                        Value = role.NameNormalized
                    });

                command.Parameters.Add(
                    new SqlParameter("@DisplayName", SqlDbType.NVarChar, 120)
                    {
                        Value = (object?)role.DisplayName ?? DBNull.Value
                    });

                command.Parameters.Add(
                    new SqlParameter("@Description", SqlDbType.NVarChar, 300)
                    {
                        Value = (object?)role.Description ?? DBNull.Value
                    });

                command.Parameters.Add(
                    new SqlParameter("@IsActive", SqlDbType.Bit)
                    {
                        Value = role.IsActive
                    });

                command.Parameters.Add(
                    new SqlParameter("@UpdatedByUserId", SqlDbType.BigInt)
                    {
                        Value = (object?)role.UpdatedByUserId ?? DBNull.Value
                    });

                var affectedRowsParameter = new SqlParameter("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(affectedRowsParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var updatedRole = await GetByIdAsync(role.RoleId, cancellationToken);
                if (updatedRole is null)
                {
                    throw new InvalidOperationException(
                        $"Role with id {role.RoleId} was updated but could not be reloaded.");
                }

                return updatedRole;
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
        long roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            return false;
        }

        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RoleDeleteProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@RoleId", SqlDbType.BigInt)
                    {
                        Value = roleId
                    });

                var affectedRowsParameter = new SqlParameter("@AffectedRows", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(affectedRowsParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);

                var affectedRows = affectedRowsParameter.Value is DBNull
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
        bool? isActive,
        CancellationToken cancellationToken)
    {
        SqlConnection? ownedConnection = null;

        try
        {
            var (command, connection) = await CreateCommandAsync(
                RoleGetRecordCountWhereDynamicProc,
                cancellationToken);

            ownedConnection = connection;

            using (command)
            {
                command.Parameters.Add(
                    new SqlParameter("@NameContains", SqlDbType.NVarChar, 80)
                    {
                        Value = !string.IsNullOrWhiteSpace(query)
                            ? query.Trim()
                            : DBNull.Value
                    });

                command.Parameters.Add(
                    new SqlParameter("@IsActive", SqlDbType.Bit)
                    {
                        Value = isActive.HasValue
                            ? isActive.Value
                            : DBNull.Value
                    });

                var scalar = await command.ExecuteScalarAsync(cancellationToken);

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
            var ambientCommand = _unitOfWork.Connection.CreateCommand();
            ambientCommand.Transaction = _unitOfWork.HasActiveTransaction
                ? _unitOfWork.Transaction
                : null;
            ambientCommand.CommandText = storedProcedureName;
            ambientCommand.CommandType = CommandType.StoredProcedure;

            return (ambientCommand, null);
        }

        var ownedConnection = _sqlConnectionFactory.CreateConnection();
        await ownedConnection.OpenAsync(cancellationToken);

        var command = ownedConnection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        return (command, ownedConnection);
    }

    private static Role MapRole(SqlDataReader reader)
    {
        return Role.Rehydrate(
            roleId: reader.GetInt64(reader.GetOrdinal("RoleId")),
            publicId: reader.GetString(reader.GetOrdinal("PublicId")),
            name: reader.GetString(reader.GetOrdinal("Name")),
            nameNormalized: reader.GetString(reader.GetOrdinal("NameNormalized")),
            displayName: ReadNullableString(reader, "DisplayName"),
            description: ReadNullableString(reader, "Description"),
            isSystem: reader.GetBoolean(reader.GetOrdinal("IsSystem")),
            isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            createdByUserId: ReadNullableInt64(reader, "CreatedByUserId"),
            updatedByUserId: ReadNullableInt64(reader, "UpdatedByUserId"));
    }

    private static RoleListResultItem MapRoleListResultItem(SqlDataReader reader)
    {
        return new RoleListResultItem
        {
            RoleId = reader.GetInt64(reader.GetOrdinal("RoleId")),
            PublicId = reader.GetString(reader.GetOrdinal("PublicId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            NameNormalized = reader.GetString(reader.GetOrdinal("NameNormalized")),
            DisplayName = ReadNullableString(reader, "DisplayName"),
            Description = ReadNullableString(reader, "Description"),
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
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long? ReadNullableInt64(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }
}